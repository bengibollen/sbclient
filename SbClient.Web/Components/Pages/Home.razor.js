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
        shouldStickToBottom: true,
        dotNetRef: null,
        lastColumns: null,
        lastRows: null,
        measureElement: null,
        resizeObserver: null
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

function ensureMeasureElement(state) {
    if (state.measureElement) {
        return state.measureElement;
    }

    const measureElement = document.createElement("span");
    measureElement.textContent = "MMMMMMMMMM";
    Object.assign(measureElement.style, {
        position: "absolute",
        visibility: "hidden",
        whiteSpace: "pre",
        left: "-9999px",
        top: "0"
    });

    document.body.appendChild(measureElement);
    state.measureElement = measureElement;
    return measureElement;
}

function parsePixels(value) {
    const pixels = Number.parseFloat(value ?? "0");
    return Number.isFinite(pixels) ? pixels : 0;
}

function measureTerminalSize(terminal, state) {
    const styles = getComputedStyle(terminal);
    const measureElement = ensureMeasureElement(state);

    measureElement.style.font = styles.font;
    measureElement.style.fontFamily = styles.fontFamily;
    measureElement.style.fontSize = styles.fontSize;
    measureElement.style.fontWeight = styles.fontWeight;
    measureElement.style.letterSpacing = styles.letterSpacing;
    measureElement.style.lineHeight = styles.lineHeight;

    const measureBounds = measureElement.getBoundingClientRect();
    const characterWidth = Math.max(measureBounds.width / measureElement.textContent.length, 1);
    const lineHeight = Math.max(
        measureBounds.height,
        parsePixels(styles.lineHeight),
        1
    );
    const paddingInline = parsePixels(styles.paddingLeft) + parsePixels(styles.paddingRight);
    const paddingBlock = parsePixels(styles.paddingTop) + parsePixels(styles.paddingBottom);

    return {
        columns: Math.max(1, Math.floor((terminal.clientWidth - paddingInline) / characterWidth)),
        rows: Math.max(1, Math.floor((terminal.clientHeight - paddingBlock) / lineHeight))
    };
}

function notifyTerminalSize(terminal, state) {
    if (!state.dotNetRef) {
        return;
    }

    const { columns, rows } = measureTerminalSize(terminal, state);
    if (columns === state.lastColumns && rows === state.lastRows) {
        return;
    }

    state.lastColumns = columns;
    state.lastRows = rows;
    state.dotNetRef.invokeMethodAsync("HandleTerminalSizeChanged", columns, rows).catch(() => {
    });
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

export function syncCommandInput(input, value) {
    if (!input) {
        return;
    }

    const nextValue = value ?? "";
    if (input.value !== nextValue) {
        input.value = nextValue;
    }

    input.focus({ preventScroll: true });
    const caretPosition = nextValue.length;
    input.setSelectionRange(caretPosition, caretPosition);
}

export function observeTerminalSize(terminal, dotNetRef) {
    if (!terminal) {
        return;
    }

    const state = ensureTerminalState(terminal);
    state.dotNetRef = dotNetRef;

    if (!state.resizeObserver) {
        state.resizeObserver = new ResizeObserver(() => {
            notifyTerminalSize(terminal, state);
        });
        state.resizeObserver.observe(terminal);
    }

    notifyTerminalSize(terminal, state);
}

export function disposeTerminalObserver(terminal) {
    if (!terminal) {
        return;
    }

    const state = trackedTerminals.get(terminal);
    if (!state) {
        return;
    }

    terminal.removeEventListener("scroll", state.handleScroll);
    state.resizeObserver?.disconnect();
    state.measureElement?.remove();
    trackedTerminals.delete(terminal);
}
