const stickThreshold = 24;
const trackedTerminals = new WeakMap();

function updateStickiness(terminal, state) {
    const distanceFromBottom = terminal.scrollHeight - terminal.clientHeight - terminal.scrollTop;
    state.shouldStickToBottom = distanceFromBottom <= stickThreshold;
}

function ensureTerminalState(terminal) {
    let state = trackedTerminals.get(terminal);
    if (state) {
        return state;
    }

    state = {
        shouldStickToBottom: true
    };

    state.handleScroll = () => {
        updateStickiness(terminal, state);
    };

    terminal.addEventListener("scroll", state.handleScroll, {
        passive: true
    });

    trackedTerminals.set(terminal, state);
    updateStickiness(terminal, state);
    return state;
}

function scrollToBottom(terminal) {
    terminal.scrollTop = terminal.scrollHeight;
}

export function syncTerminalScroll(terminal) {
    if (!terminal) {
        return;
    }

    const state = ensureTerminalState(terminal);
    if (!state.shouldStickToBottom) {
        return;
    }

    scrollToBottom(terminal);

    requestAnimationFrame(() => {
        scrollToBottom(terminal);

        requestAnimationFrame(() => {
            scrollToBottom(terminal);
            setTimeout(() => scrollToBottom(terminal), 0);
        });
    });
}
