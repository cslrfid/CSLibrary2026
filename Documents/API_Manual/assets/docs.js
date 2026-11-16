/* Renders the navigation sidebar of the API reference manual.
 *
 * The navigation is delivered as window.DOCS_NAV (assets/nav.js) so that the
 * hand written guide pages and the generated API pages share one navigation
 * without the pages having to be rewritten by the build.
 */
(function () {
    "use strict";

    function currentPage() {
        return decodeURIComponent(window.location.pathname);
    }

    function isCurrent(href) {
        var here = currentPage();
        return here === href || here.slice(-(href.length + 1)) === "/" + href;
    }

    function rootPrefix() {
        var path = window.location.pathname;
        var depth = path.split("/").length - 1;
        // The manual lives in a directory that is one level below the site root.
        var prefix = "";
        for (var index = 1; index < depth; index++) {
            prefix += "../";
        }
        return prefix;
    }

    function build() {
        var nav = window.DOCS_NAV;
        var container = document.getElementById("doc-nav");
        if (!nav || !container) {
            return;
        }

        var prefix = rootPrefix();

        var html = '<p class="nav-title">' + escapeHtml(nav.title) + "</p>";

        (nav.sections || []).forEach(function (section) {
            html += '<div class="nav-section"><p class="nav-title">' +
                escapeHtml(section.title) + "</p><ul>";

            (section.items || []).forEach(function (item) {
                var current = isCurrent(item.href) ? ' class="current"' : "";
                html += "<li><a" + current + ' href="' + prefix + item.href + '">' +
                    escapeHtml(item.title) + "</a></li>";
            });

            html += "</ul></div>";
        });

        container.innerHTML = html;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", build);
    } else {
        build();
    }
})();
