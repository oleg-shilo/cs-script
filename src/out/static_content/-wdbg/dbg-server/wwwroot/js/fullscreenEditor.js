// CodeMirror initialization for fullscreen editor
let editor;
export function initFullscreenEditor(elementId, content, theme, line) {
    const element = document.getElementById(elementId);
    if (!element) return null;

    // Create CodeMirror instance
    editor = CodeMirror(element, {
        value: content,
        mode: "text/x-csharp",
        theme: theme,
        lineNumbers: true,
        indentUnit: 4,
        tabSize: 4,
        indentWithTabs: true,
        smartIndent: true,
        lineWrapping: false,
        scrollbarStyle: "native",
        matchBrackets: true,
        autoCloseBrackets: true,
        styleActiveLine: true
    });

    // Make it fill the container
    editor.setSize("100%", "100%");

    // Refresh the editor when window is resized
    window.addEventListener('resize', () => {
        editor.refresh();
    });

    // Focus the editor
    setTimeout(() => {
        editor.focus();
    }, 100);

    return editor;
}

export function scrollToLine(line) {
    if (!editor) return;

    editor.setCursor(line, 0);

    // Select the entire line
    const lineContent = editor.getLine(line);
    if (lineContent) {
        editor.setSelection(
            { line: line, ch: 0 },
            { line: line, ch: lineContent.length }
        );
    }

    editor.scrollIntoView({ line: line, ch: 0 }, 100);
}