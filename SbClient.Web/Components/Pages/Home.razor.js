const stickThreshold = 24;

for (const terminal of document.querySelectorAll("[data-terminal-scroll]")) {
    setupTerminalAutoScroll(terminal);
}

function setupTerminalAutoScroll(terminal) {
    if (terminal.dataset.scrollObserverAttached === "true") {
        return;
    }

    terminal.dataset.scrollObserverAttached = "true";

    let shouldStickToBottom = true;

    const updateStickiness = () => {
        const distanceFromBottom = terminal.scrollHeight - terminal.clientHeight - terminal.scrollTop;
        shouldStickToBottom = distanceFromBottom <= stickThreshold;
    };

    const scrollToBottom = () => {
        terminal.scrollTop = terminal.scrollHeight;
    };

    terminal.addEventListener("scroll", updateStickiness);

    const observer = new MutationObserver(() => {
        if (!shouldStickToBottom) {
            return;
        }

        requestAnimationFrame(scrollToBottom);
    });

    observer.observe(terminal, {
        childList: true,
        subtree: true,
        characterData: true
    });

    requestAnimationFrame(() => {
        updateStickiness();

        if (shouldStickToBottom) {
            scrollToBottom();
        }
    });
}
