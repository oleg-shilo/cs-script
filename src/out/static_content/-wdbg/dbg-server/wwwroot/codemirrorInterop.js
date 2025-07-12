window.codemirrorInterop = {
    editor: null,
    dotnetRef: null,
    breakpoints: new Set(),
    currentStepLine: null,

    init: function (theme, dotnetRef) {
        this.dotnetRef = dotnetRef;
        window.editor = CodeMirror(document.getElementById('editor'), {
            mode: "text/x-csharp",
            lineNumbers: true,
            tabSize: 4,
            indentUnit: 4,
            indentWithTabs: false,
            // Add the new gutter for current step
            gutters: [
                "breakpoints",
                "CodeMirror-linenumbers",
                "CodeMirror-foldgutter",
                "current-step"
            ],

            foldGutter: true,
            extraKeys: {
                "Ctrl-Space": function (cm) { window.codemirrorInterop.showCustomCompletion(cm); },
                "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
                "Alt-Up": function (cm) { window.codemirrorInterop.moveLines(cm, -1); },
                "Alt-Down": function (cm) { window.codemirrorInterop.moveLines(cm, 1); },
            }
        });

        this.editor = window.editor;
        window.codemirrorInterop.addIndentGuideOverlay();

        this.setTheme(theme);

        this.editor.on("cursorActivity", (cm) => {
            if (this.dotnetRef) {
                const pos = cm.getCursor();
                const lineText = cm.getLine(pos.line);
                const offset = window.codemirrorInterop.getCaretAbsolutePosition();
                this.dotnetRef.invokeMethodAsync("UpdateCaretInfo", lineText, pos.line, pos.ch, offset);
            }
        });

        // Dummy completion
        this.editor.on("inputRead", function (cm, change) {
            if (change.text[0] === ".") {
                window.codemirrorInterop.showCustomCompletion(cm);
            }
        });

        this.editor.on("keydown", function (cm, event) {
            // console.log(event.ctrlKey, event.altKey, event.shiftKey, event.key, event.keyCode);

            if (event.ctrlKey && !event.shiftKey && !event.altKey && (event.key === '/')) {
                window.codemirrorInterop.toggleComment(cm);
                event.preventDefault();
                event.stopPropagation();
            }
            else if (event.ctrlKey && event.shiftKey && !event.altKey && (event.key === "Insert" || event.keyCode === 45)) {
                window.codemirrorInterop.duplicateSelectionOrLine(cm);
                event.preventDefault();
                event.stopPropagation();
            }
            else if (event.ctrlKey && event.shiftKey && !event.altKey && (event.key === "Delete" || event.keyCode === 46)) {
                if (cm.somethingSelected()) {
                    cm.replaceSelection("");
                } else {
                    cm.execCommand("deleteLine");
                }
                event.preventDefault();
                event.stopPropagation();
            }
        });

        // Breakpoint toggling
        this.editor.on("gutterClick", (cm, n) => {
            const info = cm.lineInfo(n);
            if (info.gutterMarkers && info.gutterMarkers.breakpoints) {
                // Remove marker
                cm.setGutterMarker(n, "breakpoints", null);
                this.breakpoints.delete(n);
            } else {
                // Add marker
                const marker = document.createElement("div");
                marker.style.color = "#d9534f";
                marker.style.marginLeft = "2px"; // Shift marker right by 8px
                marker.innerHTML = "●";
                cm.setGutterMarker(n, "breakpoints", marker);
                this.breakpoints.add(n);
            }
            this.dotnetRef.invokeMethodAsync("UserUpdatedBreakpoints", Array.from(this.breakpoints));
        });

        const editorElem = document.getElementById('editor');
        if (editorElem) {
            editorElem.addEventListener('wheel', function (event) {
                if (event.ctrlKey) {
                    event.preventDefault();
                    if (event.deltaY < 0) {
                        // Zoom in
                        window.codemirrorInterop.currentFontSize = Math.min(window.codemirrorInterop.currentFontSize + 1, 40);
                    } else {
                        // Zoom out
                        window.codemirrorInterop.currentFontSize = Math.max(window.codemirrorInterop.currentFontSize - 1, 8);
                    }
                    window.codemirrorInterop.setFontSize(window.codemirrorInterop.currentFontSize);
                }
            }, { passive: false });
        }

        this.editor.on("change", function () {
            if (window.codemirrorInterop && window.codemirrorInterop.dotnetRef) {
                window.codemirrorInterop.dotnetRef.invokeMethodAsync("OnEditorChanged");
            }
            window.codemirrorInterop.syncBreakpoints(); // <-- keep breakpoints in sync
            window.codemirrorInterop.renderIndentGuides();
        });

        // Initial render
        window.codemirrorInterop.renderIndentGuides();

        // Enable token tooltips
        window.codemirrorInterop.enableTokenTooltips();
    },

    showCustomCompletion: function (cm) {
        if (!cm) return;
        cm.showHint({
            hint: async function () {
                var caret = window.codemirrorInterop.getCaretAbsolutePosition();
                const fullText = window.codemirrorInterop.getValue();
                const cursor = cm.getCursor();
                const lineText = cm.getLine(cursor.line);

                // Determine if there's a dot before the cursor that we need to preserve
                const isDotCompletion = cursor.ch > 0 && lineText.charAt(cursor.ch - 1) === '.';

                var suggestions = await window.codemirrorInterop.dotnetRef
                    .invokeMethodAsync("OnCompletionRequest", fullText, caret);

                // Find any partial match at the current cursor position
                const token = cm.getTokenAt(cursor);

                // Calculate the correct start position for replacement
                let fromCh = cursor.ch;

                // If this is a dot completion, preserve the dot by starting after it
                if (isDotCompletion) {
                    fromCh = cursor.ch; // Start replacing after the dot
                }
                // Otherwise look for a token to replace
                else if (token && token.string && token.string.trim() &&
                    cursor.ch > token.start && token.type !== "comment") {
                    fromCh = token.start;
                }

                const from = { line: cursor.line, ch: fromCh };

                // Apply theme to hint container after it's created
                setTimeout(() => window.codemirrorInterop.applyThemeToHint(), 0);

                return {
                    from: from,
                    to: cursor,
                    list: suggestions ? suggestions.map(item => {
                        return {
                            ...item,
                            hint: function (cm, data, completion) {
                                cm.replaceRange(completion.text, data.from, data.to);
                                return true; // Tell CodeMirror we handled the completion
                            }
                        };
                    }) : []
                };
            },
            completeSingle: true
        });
    },

    applyThemeToHint: function () {
        const theme = this.editor ? this.editor.getOption("theme") : "default";
        const hintContainer = document.querySelector('.CodeMirror-hints');
        if (!hintContainer) return;

        // Remove any existing theme classes
        hintContainer.className = hintContainer.className
            .replace(/hint-theme-\S+/g, '')
            .trim();

        // Add appropriate theme class
        let hintThemeClass = 'hint-theme-light';

        if (theme === "dracula") hintThemeClass = 'hint-theme-dracula';
        else if (theme === "material-darker") hintThemeClass = 'hint-theme-material-darker';
        else if (theme === "monokai") hintThemeClass = 'hint-theme-monokai';
        else if (theme === "3024-night") hintThemeClass = 'hint-theme-3024-night';
        else if (theme === "abcdef") hintThemeClass = 'hint-theme-abcdef';
        else if (theme === "blackboard") hintThemeClass = 'hint-theme-blackboard';
        else if (theme === "darkone") hintThemeClass = 'hint-theme-darkone';

        hintContainer.classList.add(hintThemeClass);

        const hintItems = hintContainer.querySelectorAll('.CodeMirror-hint');
        hintItems.forEach(item => {
            item.style.paddingRight = '10px';
        });
    },

    setTheme: function (theme) {
        if (this.editor) {
            this.editor.setOption("theme", theme);
        }
        // Panel theme logic
        var panelClass = "panel-theme-light";
        if (theme === "dracula") panelClass = "panel-theme-dracula";
        else if (theme === "material-darker") panelClass = "panel-theme-material-darker";
        else if (theme === "monokai") panelClass = "panel-theme-monokai";
        else if (theme === "3024-night") panelClass = "panel-theme-3024-night";
        else if (theme === "abcdef") panelClass = "panel-theme-abcdef";
        else if (theme === "blackboard") panelClass = "panel-theme-blackboard";
        else if (theme === "darkone") panelClass = "panel-theme-darkone";

        var rightPanel = document.querySelector('.right-panel');
        var bottomPanel = document.querySelector('.left-bottom-panel');
        var sidebarPanel = document.getElementById('sidebar-panel');
        var toprowPanel = document.getElementById('top-row-panel');
        var editorElement = document.getElementById('editor');

        // Remove all theme classes first
        if (rightPanel) rightPanel.className = rightPanel.className.replace(/panel-theme-\S+/g, '').trim();
        if (bottomPanel) bottomPanel.className = bottomPanel.className.replace(/panel-theme-\S+/g, '').trim();
        if (sidebarPanel) sidebarPanel.className = sidebarPanel.className.replace(/panel-theme-\S+/g, '').trim();
        if (toprowPanel) toprowPanel.className = toprowPanel.className.replace(/panel-theme-\S+/g, '').trim();
        if (editorElement) editorElement.className = editorElement.className.replace(/panel-theme-\S+/g, '').trim();
        // Add the new theme class
        if (rightPanel) rightPanel.classList.add(panelClass);
        if (bottomPanel) bottomPanel.classList.add(panelClass);
        if (sidebarPanel) sidebarPanel.classList.add(panelClass);
        if (toprowPanel) toprowPanel.classList.add(panelClass);
        if (editorElement) editorElement.classList.add(panelClass);

        // Also apply theme to any active hint container
        this.applyThemeToHint();
    },

    toggleBreakpointAtCursor: function () {
        if (!this.editor)
            return;

        const line = this.editor.getCursor().line;
        const info = this.editor.lineInfo(line);
        if (info.gutterMarkers && info.gutterMarkers.breakpoints) {
            // Remove marker
            this.editor.setGutterMarker(line, "breakpoints", null);
            this.breakpoints.delete(line);
        } else {
            // Add marker
            const marker = document.createElement("div");
            marker.style.color = "#d9534f";
            marker.style.marginLeft = "2px";
            marker.innerHTML = "●";
            this.editor.setGutterMarker(line, "breakpoints", marker);
            this.breakpoints.add(line);
        }
        if (this.dotnetRef) {
            this.dotnetRef.invokeMethodAsync("UserUpdatedBreakpoints", Array.from(this.breakpoints));
        }
    },

    getBreakpoints: function () {
        // Return a comma-separated string of line numbers with breakpoints
        return Array.from(this.breakpoints);
    },

    syncBreakpoints: function () {
        if (!window.editor) return;
        const newBreakpoints = new Set();
        for (let i = 0; i < window.editor.lineCount(); i++) {
            const info = window.editor.lineInfo(i);
            if (info.gutterMarkers && info.gutterMarkers.breakpoints) {
                newBreakpoints.add(i);
            }
        }
        window.codemirrorInterop.breakpoints = newBreakpoints;
    },

    setBreakpoints: function (lines) {
        if (!this.editor) return;
        this.breakpoints = new Set(lines);
        for (let i = 0; i < this.editor.lineCount(); i++) {
            this.editor.setGutterMarker(i, "breakpoints", null);
        }
        for (const n of lines) {
            const marker = document.createElement("div");
            marker.style.color = "#d9534f";
            marker.style.marginLeft = "2px"; // Shift marker right by 8px
            marker.innerHTML = "●";
            this.editor.setGutterMarker(n, "breakpoints", marker);
        }
    },

    clearAllBreakpoints: function () {
        if (!window.editor) return;
        // Remove all gutter markers for breakpoints
        for (let i = 0; i < window.editor.lineCount(); i++) {
            window.editor.setGutterMarker(i, "breakpoints", null);
        }
        // Clear the breakpoints set
        if (window.codemirrorInterop.breakpoints) {
            window.codemirrorInterop.breakpoints.clear();
        }
        // Notify .NET side if needed
        if (window.codemirrorInterop.dotnetRef) {
            window.codemirrorInterop.dotnetRef.invokeMethodAsync("UserUpdatedBreakpoints", []);
        }
    },

    scrollCurrentLineToView: function () {
        if (!this.editor) return;
        const pos = this.editor.getCursor();
        this.editor.scrollIntoView({ line: pos.line, ch: 0 }, 100);
    },

    scrollToAndHighlightLine: function (line, ch) {
        if (!this.editor) return;
        this.editor.scrollIntoView({ line: line, ch: ch }, 100);
        this.editor.setCursor({ line: line, ch: ch });
        this.editor.addLineClass(line, 'background', 'cm-error-highlight');
        setTimeout(() => this.editor.removeLineClass(line, 'background', 'cm-error-highlight'), 2000);
    },

    scrollLineToView: function (line) {
        if (!window.editor) return;
        // Ensure line is within bounds
        const lineCount = window.editor.lineCount();
        if (typeof line !== "number" || line < 0 || line >= lineCount) return;
        window.editor.scrollIntoView({ line: line, ch: 0 }, 100);
    },

    getCaretAbsolutePosition: function (line, ch) {
        const cm = window.codemirrorInterop.editor;
        if (!cm) return { left: 0, top: 0, bottom: 0, characterOffset: 0 };

        const pos = cm.getCursor();
        if (line)
            pos.line = line;
        if (ch)
            pos.ch = ch;

        const fullText = window.codemirrorInterop.getValue();

        // Use the original line ending style
        const lineEndingStyle = /\r\n/.test(fullText) ? '\r\n' : '\n';
        const lineEndingLength = lineEndingStyle.length;

        let characterOffset = 0;

        for (let i = 0; i < pos.line; i++) {
            characterOffset += cm.getLine(i).length + lineEndingLength;
        }

        // Add characters in current line up to caret position
        characterOffset += pos.ch;
        return characterOffset;
    },

    setCaretAbsolutePosition: function (newOffset) {
        const cm = window.codemirrorInterop.editor;
        if (!cm) return;

        const fullText = window.codemirrorInterop.getValue();
        if (newOffset < 0 || newOffset > fullText.length) return;

        // Use the original line ending style
        const lineEndingStyle = /\r\n/.test(fullText) ? '\r\n' : '\n';
        const lineEndingLength = lineEndingStyle.length;

        let charCount = 0;
        let lineNum = 0;

        // Find the line where our offset exists
        while (lineNum < cm.lineCount()) {
            const lineText = cm.getLine(lineNum);
            const lineLength = lineText.length + lineEndingLength;

            if (charCount + lineLength > newOffset) {
                // We found the line - calculate the character position within this line
                const ch = newOffset - charCount;
                cm.setCursor({ line: lineNum, ch: ch });
                cm.scrollIntoView({ line: lineNum, ch: ch }, 100);
                return;
            }

            charCount += lineLength;
            lineNum++;
        }

        // If we get here, the offset is at the very end of the document
        const lastLine = cm.lineCount() - 1;
        const lastLineLength = cm.getLine(lastLine).length;
        cm.setCursor({ line: lastLine, ch: lastLineLength });
        cm.scrollIntoView(null, 100);
    },

    // Set the current debug step line (0-based)
    setCurrentStepLine: function (line) {
        if (!this.editor) return;
        // Remove previous marker
        if (this.currentStepLine !== null) {
            this.editor.setGutterMarker(this.currentStepLine, "current-step", null);
            this.editor.removeLineClass(this.currentStepLine, "background", "cm-current-step-line");
        }
        this.currentStepLine = line;
        if (line !== null && line >= 0 && line < this.editor.lineCount()) {
            // Create the Open Iconic play icon as the marker
            const marker = document.createElement("span");
            marker.className = "oi oi-caret-right";
            marker.style.color = "#BE6835";
            marker.style.fontSize = "12px";
            marker.style.marginLeft = "-2px";
            marker.style.marginTop = "-3px";
            marker.style.verticalAlign = "middle";

            this.editor.setGutterMarker(line, "current-step", marker);
            this.editor.addLineClass(line, "background", "cm-current-step-line");
        }
    },

    moveLines: function (cm, direction) {
        if (!cm) return;
        var ranges = cm.listSelections();
        var lines = [];
        var minLine = cm.lastLine(), maxLine = cm.firstLine();

        // Collect all unique lines in the selection
        ranges.forEach(function (range) {
            var from = range.from().line;
            var to = range.to().line;
            if (range.to().ch === 0 && from !== to) to--;
            for (var i = from; i <= to; i++) {
                if (lines.indexOf(i) === -1) lines.push(i);
                if (i < minLine) minLine = i;
                if (i > maxLine) maxLine = i;
            }
        });

        if (direction < 0 && minLine === 0) return; // Can't move up
        if (direction > 0 && maxLine === cm.lastLine()) return; // Can't move down

        // Sort lines for correct movement
        lines.sort(function (a, b) { return direction < 0 ? a - b : b - a; });

        cm.operation(function () {
            lines.forEach(function (line) {
                var swapLine = line + direction;
                var text = cm.getLine(line);
                var swapText = cm.getLine(swapLine);
                cm.replaceRange(swapText, { line: line, ch: 0 }, { line: line, ch: text.length });
                cm.replaceRange(text, { line: swapLine, ch: 0 }, { line: swapLine, ch: swapText.length });
            });

            // Move selection
            var newRanges = ranges.map(function (range) {
                var anchor = { line: range.anchor.line + direction, ch: range.anchor.ch };
                var head = { line: range.head.line + direction, ch: range.head.ch };
                return { anchor: anchor, head: head };
            });
            cm.setSelections(newRanges);
        });
    },

    duplicateSelectionOrLine: function (cm) {
        if (!cm) return;
        cm.operation(function () {
            var ranges = cm.listSelections();
            var newSelections = [];
            for (var i = 0; i < ranges.length; i++) {
                var range = ranges[i];
                if (range.empty()) {
                    // Duplicate the current line
                    var line = range.head.line;
                    var lineText = cm.getLine(line);
                    cm.replaceRange(lineText + "\n", { line: line, ch: 0 });
                    newSelections.push({
                        anchor: { line: line + 1, ch: range.head.ch },
                        head: { line: line + 1, ch: range.head.ch }
                    });
                } else {
                    // Duplicate the selected text on a new line below the selection
                    var from = range.from();
                    var to = range.to();
                    var text = ' '.repeat(from.ch) + cm.getRange(from, to);
                    // Insert at the start of the line after the selection's last line
                    // var insertPos = { line: to.line + 1, ch: from.ch };
                    var insertPos = { line: to.line + 1, ch: 0 };
                    cm.replaceRange(text + "\n", insertPos);
                    newSelections.push({
                        anchor: { line: insertPos.line, ch: from.ch },
                        head: { line: insertPos.line + (to.line - from.line), ch: to.ch }
                    });
                }
            }
            cm.setSelections(newSelections);
        });
    },

    toggleComment: function (cm) {
        if (!cm) return;

        // Get selection range
        const selections = cm.listSelections();
        cm.operation(function () {
            selections.forEach(function (sel) {
                const fromLine = Math.min(sel.anchor.line, sel.head.line);
                const toLine = Math.max(sel.anchor.line, sel.head.line);

                // Determine if all lines are commented
                let allCommented = true;
                for (let i = fromLine; i <= toLine; i++) {
                    const lineText = cm.getLine(i);
                    if (!/^\s*\/\//.test(lineText)) {
                        allCommented = false;
                        break;
                    }
                }

                // Toggle comment
                for (let i = fromLine; i <= toLine; i++) {
                    const lineText = cm.getLine(i);
                    if (allCommented) {
                        // Uncomment: remove first // after leading whitespace
                        cm.replaceRange(
                            lineText.replace(/^(\s*)\/\/\s?/, "$1"),
                            { line: i, ch: 0 },
                            { line: i, ch: lineText.length }
                        );
                    } else {
                        // Comment: add // after leading whitespace
                        cm.replaceRange(
                            lineText.replace(/^(\s*)/, "$1// "),
                            { line: i, ch: 0 },
                            { line: i, ch: lineText.length }
                        );
                    }
                }
            });
        });
    },

    getValue: function () {
        if (window.editor) {
            let content = window.editor.getValue();

            // Determine the appropriate line ending for the current OS
            const osLineEnding = navigator.platform.toLowerCase().includes('win') ? '\r\n' : '\n';

            // If we stored the original line ending when the content was loaded, use that
            // Otherwise, use the OS default
            const targetLineEnding = window.codemirrorInterop.originalLineEnding || osLineEnding;

            // Convert line endings if they don't match the target
            if (targetLineEnding === '\r\n' && !content.includes('\r\n')) {
                // Convert \n to \r\n
                content = content.replace(/\r?\n/g, '\r\n');
            } else if (targetLineEnding === '\n' && content.includes('\r\n')) {
                // Convert \r\n to \n
                content = content.replace(/\r\n/g, '\n');
            }

            return content;
        }
        return "";
    },

    saveTextAsFile: function (text, filename) {
        const blob = new Blob([text], { type: "text/plain" });
        const link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    enableTokenTooltips: function () {
        const cm = window.codemirrorInterop.editor;
        if (!cm) return;

        // Remove any previous handlers to avoid duplicates
        if (cm._tokenTooltipHandler) {
            cm.getWrapperElement().removeEventListener('mousemove', cm._tokenTooltipHandler);
            cm.getWrapperElement().removeEventListener('mouseleave', cm._tokenTooltipLeaveHandler);
        }

        let tooltipDiv = null;
        let lastToken = null;
        let mouseOverTooltip = false;
        let tooltipTimeout = null;
        let tooltipShowDelay = null; // Delay timer for showing tooltip
        let currentToken = null; // Track the current token for better hover detection

        function showTooltip(text, x, y) {
            hideTooltip();

            tooltipDiv = document.createElement('div');
            tooltipDiv.className = 'cm-tooltip-content';
            tooltipDiv.innerHTML = text;
            tooltipDiv.style.position = 'fixed';
            tooltipDiv.style.left = (x + 10) + 'px';
            tooltipDiv.style.top = (y + 10) + 'px';
            tooltipDiv.style.zIndex = 10000;
            tooltipDiv.style.padding = '8px';
            tooltipDiv.style.boxSizing = 'border-box';
            tooltipDiv.style.width = 'auto';
            tooltipDiv.style.height = 'auto';
            tooltipDiv.style.overflow = 'auto';
            tooltipDiv.style.maxWidth = 'none';
            tooltipDiv.style.border = '1px solid rgba(0,0,0,0.2)';
            tooltipDiv.style.borderRadius = '4px';
            tooltipDiv.style.boxShadow = '0 2px 5px rgba(0,0,0,0.2)';

            // Apply the current theme to the tooltip
            const theme = window.codemirrorInterop.editor ?
                window.codemirrorInterop.editor.getOption("theme") : "default";

            if (theme === "dracula") tooltipDiv.classList.add('panel-theme-dracula');
            else if (theme === "material-darker") tooltipDiv.classList.add('panel-theme-material-darker');
            else if (theme === "monokai") tooltipDiv.classList.add('panel-theme-monokai');
            else if (theme === "3024-night") tooltipDiv.classList.add('panel-theme-3024-night');
            else if (theme === "abcdef") tooltipDiv.classList.add('panel-theme-abcdef');
            else if (theme === "blackboard") tooltipDiv.classList.add('panel-theme-blackboard');
            else if (theme === "darkone") tooltipDiv.classList.add('panel-theme-darkone');
            else tooltipDiv.classList.add('panel-theme-light');

            // Add event listeners for tooltip hover
            tooltipDiv.addEventListener('mouseenter', function () {
                mouseOverTooltip = true;
                clearTimeout(tooltipTimeout);
            });

            tooltipDiv.addEventListener('mouseleave', function () {
                mouseOverTooltip = false;
                // Start a timer to hide the tooltip when mouse leaves
                tooltipTimeout = setTimeout(hideTooltip, 1000);
            });

            document.body.appendChild(tooltipDiv);
            window.lastTooltip = tooltipDiv; // Store reference to close it later
            window.lastToken = lastToken; // Store the token information globally
        }

        function hideTooltip() {
            if (tooltipDiv) {
                if (!mouseOverTooltip) {
                    document.body.removeChild(tooltipDiv);
                    tooltipDiv = null;
                }
            }
        }

        cm._tokenTooltipHandler = async function (e) {
            const { left, top } = cm.getWrapperElement().getBoundingClientRect();
            const x = e.clientX - left, y = e.clientY - top;
            const pos = cm.coordsChar({ left: e.clientX, top: e.clientY });
            const token = cm.getTokenAt(pos);

            // Only proceed if mouse is over a valid token
            if (!token || !token.string.trim() || pos.ch < token.start || pos.ch >= token.end) {
                // Clear delay timer when mouse leaves token
                clearTimeout(tooltipShowDelay);
                currentToken = null;

                // Don't hide immediately if mouse is over tooltip
                if (!mouseOverTooltip) {
                    tooltipTimeout = setTimeout(function () {
                        if (!mouseOverTooltip) {
                            hideTooltip();
                            lastToken = null;
                        }
                    }, 300);
                }
                return;
            }

            // Convert token to a comparable string for tracking
            const tokenKey = `${pos.line}:${token.start}:${token.end}:${token.string}`;

            // If this is a new token, start the delay timer
            if (currentToken !== tokenKey) {
                // Clear any existing delay timer
                clearTimeout(tooltipShowDelay);

                // Set current token
                currentToken = tokenKey;

                // Start new delay timer
                tooltipShowDelay = setTimeout(async function () {
                    // If mouse is still over the same token after delay
                    if (currentToken === tokenKey) {
                        lastToken = {
                            start: token.start,
                            end: token.end,
                            line: pos.line,
                            ch: pos.ch,
                            string: token.string
                        };

                        var tooltipText = await window.codemirrorInterop.dotnetRef.invokeMethodAsync(
                            "GetTooltipFor",
                            token.string,
                            pos.line,
                            pos.ch
                        );

                        if (tooltipText)
                            showTooltip(tooltipText, e.clientX, e.clientY);
                    }
                }, 1000); // 1 second delay
            }
        };

        cm._tokenTooltipLeaveHandler = function () {
            // Clear the delay timer when leaving the editor
            clearTimeout(tooltipShowDelay);
            currentToken = null;

            // Use timeout to allow moving to tooltip
            tooltipTimeout = setTimeout(function () {
                if (!mouseOverTooltip) {
                    hideTooltip();
                    lastToken = null;
                }
            }, 300);
        };

        cm.getWrapperElement().addEventListener('mousemove', cm._tokenTooltipHandler);
        cm.getWrapperElement().addEventListener('mouseleave', cm._tokenTooltipLeaveHandler);
    },

    batchDocumentUpdate: function (content, breakpoints) {
        if (!this.editor) return;

        // Disable events during batch update
        this.editor.off("change");

        try {
            // Update content
            this.setValue(content);

            // Update breakpoints
            this.setBreakpoints(breakpoints);

            // Refresh editor once
            this.editor.refresh();
        } finally {
            // Re-enable events
            this.editor.on("change", function () {
                if (window.codemirrorInterop && window.codemirrorInterop.dotnetRef) {
                    window.codemirrorInterop.dotnetRef.invokeMethodAsync("OnEditorChanged");
                }
                window.codemirrorInterop.syncBreakpoints();
                window.codemirrorInterop.renderIndentGuides();
            });
        }
    },
};

// =============================

window.codemirrorInterop.setValue = function (content) {
    if (window.editor && typeof window.editor.setValue === "function") {
        // Store the original line ending style for later use
        window.codemirrorInterop.originalLineEnding = /\r\n/.test(content) ? '\r\n' : '\n';
        window.codemirrorInterop.lineEndingLength = window.codemirrorInterop.originalLineEnding.length;

        // CodeMirror will normalize to \n internally, but we now have the original style saved
        window.editor.setValue(content);
    } else {
        console.warn("window.editor is", window.editor);
        console.warn("typeof setValue is", typeof window.editor?.setValue);
        alert("CodeMirror editor is not initialized or setValue is not a function.");
    }
};

window.setRightPanelWidth = function (newLeftWidth) {
    const container = document.querySelector('.split-container');
    const left = container.querySelector('.left-panel');
    const right = container.querySelector('.right-panel');
    const resizer = container.querySelector('.split-resizer');
    const containerRect = container.getBoundingClientRect();

    // Set min/max widths
    const minLeft = 100, minRight = 100;
    const resizerWidth = resizer ? resizer.offsetWidth : 4;
    const maxLeft = containerRect.width - minRight - resizerWidth;
    if (newLeftWidth < minLeft) newLeftWidth = minLeft;
    if (newLeftWidth > maxLeft) newLeftWidth = maxLeft;

    left.style.width = newLeftWidth + 'px';

    // Calculate and store right panel width (from right edge)
    const rightPanelWidth = containerRect.width - newLeftWidth - resizerWidth;
    localStorage.setItem("split_right_width", rightPanelWidth);

    if (window.codemirrorInterop && window.codemirrorInterop.editor) {
        window.codemirrorInterop.editor.refresh();
    }
}

window.restoreSplitPositions = function () {
    // Restore vertical split (from right edge)
    const container = document.querySelector('.split-container');
    const left = container?.querySelector('.left-panel');
    const right = container?.querySelector('.right-panel');
    const resizer = container?.querySelector('.split-resizer');
    const containerRect = container?.getBoundingClientRect();
    const rightPanelWidth = parseInt(localStorage.getItem("split_right_width"), 10);

    if (left && container && resizer && !isNaN(rightPanelWidth)) {
        const resizerWidth = resizer.offsetWidth || 4;
        const minLeft = 100, minRight = 100;
        const maxLeft = containerRect.width - minRight - resizerWidth;
        let newLeftWidth = containerRect.width - rightPanelWidth - resizerWidth;
        if (newLeftWidth < minLeft) newLeftWidth = minLeft;
        if (newLeftWidth > maxLeft) newLeftWidth = maxLeft;
        left.style.width = newLeftWidth + "px";
    }

    // Restore horizontal split (unchanged)
    const leftVertical = left?.querySelector('.left-vertical-container');
    const topPanel = leftVertical?.querySelector('.left-top-panel');
    const bottomPanel = leftVertical?.querySelector('.left-bottom-panel');
    const hResizer = leftVertical?.querySelector('.left-horizontal-resizer');

    const bottomHeight = localStorage.getItem("split_left_bottom_height");
    if (topPanel && bottomPanel && hResizer && bottomHeight) {
        const containerRect = leftVertical.getBoundingClientRect();
        const resizerHeight = hResizer.offsetHeight || 4;
        let newTopHeight = containerRect.height - parseInt(bottomHeight, 10) - resizerHeight;
        // Enforce min/max
        const minTop = 40, minBottom = 30;
        const maxTop = containerRect.height - minBottom;
        if (newTopHeight < minTop) newTopHeight = minTop;
        if (newTopHeight > maxTop) newTopHeight = maxTop;
        topPanel.style.height = newTopHeight + "px";
        bottomPanel.style.height = (containerRect.height - newTopHeight - resizerHeight) + "px";
    }

    if (window.codemirrorInterop && window.codemirrorInterop.editor) {
        window.codemirrorInterop.editor.refresh();
    }
};

// Debounced restore on resize (including maximize)
let resizeTimeout;
window.addEventListener('resize', function () {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        window.restoreSplitPositions();
    }, 100); // 100ms delay to catch maximize/final size
});

window.startSplitResize = function () {
    const container = document.querySelector('.split-container');
    const left = container.querySelector('.left-panel');
    const resizer = container.querySelector('.split-resizer');
    const right = container.querySelector('.right-panel');

    function onMouseMove(e) {
        const containerRect = container.getBoundingClientRect();
        let newLeftWidth = e.clientX - containerRect.left;
        setRightPanelWidth(newLeftWidth);
    }

    function onMouseUp() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
};

window.startLeftHorizontalResize = function () {
    const container = document.querySelector('.left-vertical-container');
    const topPanel = container.querySelector('.left-top-panel');
    const resizer = container.querySelector('.left-horizontal-resizer');
    const bottomPanel = container.querySelector('.left-bottom-panel');
    const totalHeight = container.getBoundingClientRect().height;

    function onMouseMove(e) {
        const containerRect = container.getBoundingClientRect();
        let newTopHeight = e.clientY - containerRect.top;
        const minTop = 40, minBottom = 30;
        const maxTop = containerRect.height - minBottom;
        if (newTopHeight < minTop) newTopHeight = minTop;
        if (newTopHeight > maxTop) newTopHeight = maxTop;
        topPanel.style.height = newTopHeight + 'px';
        const newBottomHeight = containerRect.height - newTopHeight - resizer.offsetHeight;
        bottomPanel.style.height = newBottomHeight + 'px';
        localStorage.setItem("split_left_bottom_height", newBottomHeight);
        bottomPanel.style.height = (containerRect.height - newTopHeight - resizer.offsetHeight) + 'px';
        // Refresh CodeMirror layout if needed
        if (window.codemirrorInterop && window.codemirrorInterop.editor) {
            window.codemirrorInterop.editor.refresh();
        }
    }

    function onMouseUp() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
};

window.readFileAsTextWithName = async function (input) {
    return new Promise((resolve, reject) => {
        if (!input || !input.files || input.files.length === 0) {
            resolve({ name: "", content: "" });
            return;
        }
        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = function (e) {
            resolve({ name: file.name, content: e.target.result });
        };
        reader.onerror = function (e) {
            reject(e);
        };
        reader.readAsText(file);
    });
};

window.autoSizeInput = function (element) {
    if (!element) return;
    // Create a temporary span to measure the text width
    var span = document.createElement('span');
    span.style.visibility = 'hidden';
    span.style.position = 'fixed';
    span.style.whiteSpace = 'pre';
    span.style.font = getComputedStyle(element).font;
    span.textContent = element.value || element.placeholder || '';
    document.body.appendChild(span);
    // Add some extra space for caret and padding
    var newWidth = (span.offsetWidth + 24);
    newWidth = Math.max(300, Math.min(newWidth, 500));
    element.style.width = newWidth + 'px';
    element.style.minWidth = '300px';
    element.style.maxWidth = '500px';
    document.body.removeChild(span);

    // Scroll to the right to show the end of the file name
    // Use setTimeout to ensure the width is applied before scrolling
    setTimeout(function () {
        element.scrollLeft = element.scrollWidth;
    }, 0);
};

window.codemirrorInterop = window.codemirrorInterop || {};

window.codemirrorInterop.setFontSize = function (size) {
    window.codemirrorInterop.currentFontSize = size;
    var editorElem = document.getElementById('editor');
    if (editorElem) {
        editorElem.style.fontSize = size + "px";
        // If CodeMirror instance is inside, update its scroller too
        var cm = editorElem.CodeMirror || (editorElem.firstElementChild && editorElem.firstElementChild.CodeMirror);
        if (cm && cm.getWrapperElement) {
            cm.getWrapperElement().style.fontSize = size + "px";
            cm.refresh();
        }
    }
};

window.codemirrorInterop.currentFontSize = 14; // default font size

window.registerWindowKeyHandlers = function (dotNetRef) {
    document.addEventListener('keydown', function (e) {
        // Ctrl+S
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnCtrlS');
        }
        else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'e') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnCtrlE');
        }
        else if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === 'f') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnCtrlF');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F4" || e.keyCode === 115)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnF4');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F5" || e.keyCode === 116)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnF5');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && e.shiftKey && (e.key === "F5" || e.keyCode === 116)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnShiftF5');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F9" || e.keyCode === 120)) {
            e.preventDefault();
            window.codemirrorInterop.toggleBreakpointAtCursor();
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && e.shiftKey && (e.key === "F9" || e.keyCode === 120)) {
            e.preventDefault();
            window.codemirrorInterop.clearAllBreakpoints();
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F7" || e.keyCode === 118)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnF7');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F10" || e.keyCode === 121)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnF10');
        }
        else if (!e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F11" || e.keyCode === 122)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnF11');
        }
        else if (e.ctrlKey && !e.metaKey && !e.altKey && !e.shiftKey && (e.key === "F12" || e.keyCode === 123)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnCtrlF12');
        }
    });
}

window.codemirrorInterop.renderIndentGuides = function () {
    if (!window.editor) return;
    const cm = window.editor;
    cm.operation(function () {
        // Remove old guides
        cm.getAllMarks().forEach(m => {
            if (m.className === "cm-indent-guide") m.clear();
        });

        for (let i = 0; i < cm.lineCount(); i++) {
            const line = cm.getLine(i);
            let match = line.match(/^(\s+)/);
            if (!match) continue;
            let spaces = match[1];
            let tabSize = cm.getOption("tabSize") || 4;
            let indent = 0;
            for (let j = 0; j < spaces.length; j++) {
                indent += spaces[j] === "\t" ? tabSize : 1;
            }
            for (let col = tabSize; col <= indent; col += tabSize) {
                cm.markText(
                    { line: i, ch: col - tabSize },
                    { line: i, ch: col - tabSize + 1 },
                    { className: "cm-indent-guide", inclusiveLeft: true, inclusiveRight: true }
                );
            }
        }
    });
};

window.codemirrorInterop.addIndentGuideOverlay = function () {
    if (!window.editor) return;
    const tabSize = window.editor.getOption("tabSize") || 4;

    // Remove previous overlays (if any)
    if (window.codemirrorInterop._indentGuideOverlay) {
        window.editor.removeOverlay(window.codemirrorInterop._indentGuideOverlay);
    }

    // Define the overlay
    const overlay = {
        token: function (stream) {
            if (stream.sol()) {
                let spaces = 0;
                while (!stream.eol()) {
                    const ch = stream.peek();
                    if (ch === " ") {
                        spaces++;
                        stream.next();
                    } else if (ch === "\t") {
                        spaces += tabSize;
                        stream.next();
                    } else {
                        break;
                    }
                }
                // For each indent level, return a style for the first char of each level
                if (spaces > 0) {
                    // Only mark the first char of each indent level
                    let col = 0;
                    stream.backUp(spaces);
                    for (let i = tabSize; i <= spaces; i += tabSize) {
                        if (col === stream.pos) {
                            stream.next();
                            return "cm-indent-guide-overlay";
                        }
                        col++;
                        stream.next();
                    }
                    stream.skipToEnd();
                }
            }
            stream.skipToEnd();
            return null;
        }
    };

    window.editor.addOverlay(overlay);
    window.codemirrorInterop._indentGuideOverlay = overlay;
};

window.getOrCreateTabId = function () {
    let tabId = sessionStorage.getItem('tabId');
    if (!tabId) {
        tabId = crypto.randomUUID();
        sessionStorage.setItem('tabId', tabId);
    }
    return tabId;
};

window.goToDefinitionFromTooltip = function (line, charPos) {
    // Close the tooltip
    if (window.lastTooltip) {
        window.lastTooltip.remove();
        window.lastTooltip = null;
    }

    // Call the C# method to go to definition
    if (window.codemirrorInterop && window.codemirrorInterop.dotnetRef) {
        window.codemirrorInterop.dotnetRef.invokeMethodAsync('GoToDefinitionFromTooltip', line, charPos);
    }
};

window.codemirrorInterop.setCaretPosition = function (line, ch, scroll) {
    const cm = window.codemirrorInterop.editor;
    if (!cm) return;

    // Ensure line is within bounds
    const lineCount = cm.lineCount();
    if (line < 0) line = 0;
    if (line >= lineCount) line = lineCount - 1;

    // Ensure character position is within bounds
    const lineText = cm.getLine(line);
    if (ch < 0) ch = 0;
    if (ch > lineText.length) ch = lineText.length;

    // Set cursor position
    cm.setCursor({ line: line, ch: ch });

    if (scroll) {
        // Scroll to make the cursor visible
        cm.scrollIntoView({ line: line, ch: ch }, 100);
    }
};

window.codemirrorInterop.triggerCompletion = function () {
    if (window.editor) {
        // Use the existing showCustomCompletion method with the current editor instance
        window.codemirrorInterop.showCustomCompletion(window.editor);
        return true;
    }
    return false;
};