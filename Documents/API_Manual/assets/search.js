/* The search of the API reference manual.
 *
 * The index is delivered as window.DOCS_INDEX (searchindex.js) and is loaded
 * with a script tag, so that the search also works when the manual is opened
 * directly from the file system.
 */
(function () {
    "use strict";

    function tokenize(text) {
        var tokens = [];
        if (!text) {
            return tokens;
        }

        var words = String(text).split(/[\s ]+/);
        for (var index = 0; index < words.length; index++) {
            var word = words[index];
            var cleaned = word.toLowerCase().replace(/[^a-z0-9]+/g, " ");
            var parts = cleaned.split(" ");
            for (var part = 0; part < parts.length; part++) {
                if (parts[part].length > 1) {
                    tokens.push(parts[part]);
                }
            }

            // A camel case identifier is also indexed by its parts.
            var current = "";
            for (var character = 0; character < word.length; character++) {
                var letter = word.charAt(character);
                if (letter >= "A" && letter <= "Z" && current.length > 0) {
                    if (current.length > 1) {
                        tokens.push(current.toLowerCase());
                    }
                    current = "";
                }

                if (/[a-zA-Z0-9]/.test(letter)) {
                    current += letter.toLowerCase();
                }
            }

            if (current.length > 1) {
                tokens.push(current);
            }
        }

        return tokens;
    }

    function search(query) {
        var index = window.DOCS_INDEX;
        if (!index || !index.pages) {
            return [];
        }

        var tokens = tokenize(query);
        if (tokens.length === 0) {
            return [];
        }

        var scores = {};
        var matched = {};

        for (var token = 0; token < tokens.length; token++) {
            var postings = index.postings[tokens[token]];
            if (!postings) {
                continue;
            }

            for (var entry = 0; entry < postings.length; entry++) {
                var pageIndex = postings[entry][0];
                var count = postings[entry][1];
                scores[pageIndex] = (scores[pageIndex] || 0) + count;
                matched[pageIndex] = (matched[pageIndex] || 0) + 1;
            }
        }

        var results = [];
        for (var key in scores) {
            if (!Object.prototype.hasOwnProperty.call(scores, key)) {
                continue;
            }

            var page = index.pages[key];
            // A page that contains every word of the query is more useful than
            // a page that contains one of them many times.
            var score = scores[key] * matched[key];
            results.push({ page: page, score: score, matched: matched[key] });
        }

        results.sort(function (left, right) {
            if (right.matched !== left.matched) {
                return right.matched - left.matched;
            }

            return right.score - left.score;
        });

        return results;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function snippet(text, terms) {
        if (!text) {
            return "";
        }

        var lower = text.toLowerCase();
        var position = -1;

        for (var index = 0; index < terms.length; index++) {
            var found = lower.indexOf(terms[index]);
            if (found >= 0 && (position < 0 || found < position)) {
                position = found;
            }
        }

        if (position < 0) {
            return escapeHtml(text.substring(0, 180)) + (text.length > 180 ? "..." : "");
        }

        var start = Math.max(0, position - 60);
        var end = Math.min(text.length, position + 140);
        var piece = text.substring(start, end);

        var highlighted = escapeHtml(piece);
        for (var term = 0; term < terms.length; term++) {
            var pattern = new RegExp("(" + terms[term].replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + ")", "gi");
            highlighted = highlighted.replace(pattern, "<mark>$1</mark>");
        }

        return (start > 0 ? "..." : "") + highlighted + (end < text.length ? "..." : "");
    }

    function render(query) {
        var list = document.getElementById("search-results");
        var status = document.getElementById("search-status");
        if (!list) {
            return;
        }

        list.innerHTML = "";

        if (!query) {
            status.textContent = "Type a word to search the manual.";
            return;
        }

        var results = search(query);
        if (results.length === 0) {
            status.textContent = "No result for \"" + query + "\".";
            return;
        }

        var terms = tokenize(query);
        status.textContent = results.length + " result" + (results.length === 1 ? "" : "s") + " for \"" + query + "\".";

        for (var index = 0; index < results.length && index < 60; index++) {
            var result = results[index];
            var page = result.page;

            var item = document.createElement("li");
            var title = document.createElement("div");
            title.className = "hit-title";

            var link = document.createElement("a");
            link.href = page[0];
            link.textContent = page[1];
            title.appendChild(link);

            if (page[2] && page[2] !== page[1]) {
                var section = document.createElement("span");
                section.className = "hit-section";
                section.textContent = page[2];
                title.appendChild(section);
            }

            item.appendChild(title);

            var text = document.createElement("div");
            text.className = "hit-snippet";
            text.innerHTML = snippet(page[3], terms);
            item.appendChild(text);

            list.appendChild(item);
        }
    }

    function start() {
        var input = document.getElementById("search-input");
        var form = document.getElementById("search-form");
        if (!input) {
            return;
        }

        var parameters = window.location.search.replace(/^\?/, "").split("&");
        for (var index = 0; index < parameters.length; index++) {
            var pair = parameters[index].split("=");
            if (pair[0] === "q") {
                input.value = decodeURIComponent(pair[1] || "");
            }
        }

        var timer = null;
        input.addEventListener("input", function () {
            window.clearTimeout(timer);
            timer = window.setTimeout(function () {
                render(input.value);
            }, 120);
        });

        if (form) {
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                render(input.value);
            });
        }

        render(input.value);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
