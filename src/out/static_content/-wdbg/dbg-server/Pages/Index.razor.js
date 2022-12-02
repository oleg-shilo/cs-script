export function loadCss(cssPath) {
    var ss = document.styleSheets;
    for (var i = 0, max = ss.length; i < max; i++) {
        if (ss[i].href.includes(cssPath.substr(2)))
            return;
    }
    var link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = cssPath;

    document.getElementsByTagName("head")[0].appendChild(link);
}

export function initTextarea() {
    $(".lined").linedtextarea({ selectedLine: -1, bpLine: 31 });
    return "OK";
}

(function ($) {
    $.fn.linedtextarea = function (options) {
        // Get the Options
        var opts = $.extend({}, $.fn.linedtextarea.defaults, options);

        /*
         * Helper function to make sure the line numbers are always
         * kept up to the current system
         */
        var fillOutLines = function (codeLines, h, lineNo) {
            while ((codeLines.height() - h) <= 0) {
                var slectedLineIndicator =
                    "<svg class='svg-icon1' viewBox='0 0 512 312' width='8pt' height='8pt'  >" +
                    "  <g transform='translate(-50, -50)'>" +
                    "    <path fill='white' d='M334.5 414c8.8 3.8 19 2 26-4.6l144-136c4.8-4.5 7.5-10.8 7.5-17.4s-2.7-12.9-7.5-17.4l-144-136c-7-6.6-17.2-8.4-26-4.6s-14.5 12.5-14.5 22l0 88L32 208c-17.7 0-32 14.3-32 32l0 32c0 17.7 14.3 32 32 32l288 0 0 88c0 9.6 5.7 18.2 14.5 22z'/>" +
                    "  </g>" +
                    "</svg>";
                var breakpointIndicator = "<svg class='svg-icon' viewBox='0 0 10 10' width='5' y='0'> <circle cx='5' cy='5' r='4' fill='red' /></svg>";
                var className = 'lineno';

                if (lineNo == opts.bpLine || lineNo == opts.selectedLine) {
                    if (lineNo == opts.selectedLine) {
                        className += " lineselect";
                    }
                    if (lineNo == opts.bpLine) {
                        slectedLineIndicator += breakpointIndicator;
                    }
                }

                codeLines.append("<div id=\"line" + lineNo + "\" class='" + className + "'>" + slectedLineIndicator + lineNo + "</div>");

                lineNo++;
            }
            return lineNo;
        };

        /*
         * Iterate through each of the elements are to be applied to
         */
        return this.each(function () {
            var lineNo = 1;
            var textarea = $(this);

            /* Turn off the wrapping of as we don't want to screw up the line numbers */
            textarea.attr("wrap", "off");
            textarea.css({ resize: 'none' });
            var originalTextAreaWidth = textarea.outerWidth();

            /* Wrap the text area in the elements we need */
            textarea.wrap("<div class='linedtextarea'></div>");
            var linedTextAreaDiv = textarea.parent().wrap("<div class='linedwrap' style='width:" + originalTextAreaWidth + "px'></div>");
            var linedWrapDiv = linedTextAreaDiv.parent();

            linedWrapDiv.prepend("<div class='lines' style='width:55px'></div>");

            var linesDiv = linedWrapDiv.find(".lines");
            linesDiv.height(textarea.height() + 6);

            /* Draw the number bar; filling it out where necessary */
            linesDiv.append("<div class='codelines'></div>");
            var codeLinesDiv = linesDiv.find(".codelines");
            lineNo = fillOutLines(codeLinesDiv, linesDiv.height(), 1);

            /* Move the textarea to the selected line */
            if (opts.selectedLine != -1 && !isNaN(opts.selectedLine)) {
                var fontSize = parseInt(textarea.height() / (lineNo - 2));
                var position = parseInt(fontSize * opts.selectedLine) - (textarea.height() / 2);
                textarea[0].scrollTop = position;
            }

            /* Set the width */
            var sidebarWidth = linesDiv.outerWidth();
            var paddingHorizontal = parseInt(linedWrapDiv.css("border-left-width")) + parseInt(linedWrapDiv.css("border-right-width")) + parseInt(linedWrapDiv.css("padding-left")) + parseInt(linedWrapDiv.css("padding-right"));
            var linedWrapDivNewWidth = originalTextAreaWidth - paddingHorizontal;
            var textareaNewWidth = originalTextAreaWidth - sidebarWidth - paddingHorizontal - 20;

            textarea.width(textareaNewWidth);
            linedWrapDiv.width(linedWrapDivNewWidth);

            /* React to the scroll event */
            textarea.scroll(function (tn) {
                var domTextArea = $(this)[0];
                var scrollTop = domTextArea.scrollTop;
                var clientHeight = domTextArea.clientHeight;
                codeLinesDiv.css({ 'margin-top': (-1 * scrollTop) + "px" });
                lineNo = fillOutLines(codeLinesDiv, scrollTop + clientHeight, lineNo);
            });

            /* Should the textarea get resized outside of our control */
            textarea.resize(function (tn) {
                var domTextArea = $(this)[0];
                linesDiv.height(domTextArea.clientHeight + 6);
            });
        });
    };

    // default options
    $.fn.linedtextarea.defaults = {
        selectedLine: -1,
        selectedClass: 'lineselect'
    };
})(jQuery);

export function setCurrentStep(lineNo) {
    $('.lineno.lineselect').removeClass('lineselect');
    if (lineNo != -1)
        $('#line' + lineNo).addClass('lineselect');
}

export function showPrompt(message) {
    return prompt(message, 'Type anything here');
}