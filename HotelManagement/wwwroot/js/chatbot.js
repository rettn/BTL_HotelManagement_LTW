(() => {
    const root = document.querySelector('.hotel-chatbot');
    if (!root) return;

    const toggle = root.querySelector('.hotel-chatbot-toggle');
    const panel = root.querySelector('.hotel-chatbot-panel');
    const close = root.querySelector('.hotel-chatbot-close');
    const form = root.querySelector('.hotel-chatbot-form');
    const input = root.querySelector('.hotel-chatbot-input');
    const messages = root.querySelector('.hotel-chatbot-messages');
    const mode = root.dataset.chatbotMode || 'customer';

    const appendMessage = (text, type) => {
        const item = document.createElement('div');
        item.className = `hotel-chatbot-message ${type}`;
        item.textContent = text;
        messages.appendChild(item);
        messages.scrollTop = messages.scrollHeight;
    };

    const setOpen = open => {
        panel.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
        if (open) input.focus();
    };

    toggle.addEventListener('click', () => setOpen(panel.hidden));
    close.addEventListener('click', () => setOpen(false));

    form.addEventListener('submit', async event => {
        event.preventDefault();
        const message = input.value.trim();
        if (!message) return;

        appendMessage(message, 'user');
        input.value = '';
        input.disabled = true;
        form.querySelector('button').disabled = true;

        const payload = { message };
        const checkIn = root.querySelector('.chatbot-check-in')?.value;
        const checkOut = root.querySelector('.chatbot-check-out')?.value;
        if (mode === 'customer' && checkIn && checkOut) {
            payload.checkIn = checkIn;
            payload.checkOut = checkOut;
        }

        try {
            const response = await fetch('/api/chatbot/message', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || 'Không thể nhận phản hồi từ chatbot.');
            appendMessage(data.reply || 'Chatbot chưa có câu trả lời.', 'bot');
        } catch (error) {
            appendMessage(error.message || 'Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        } finally {
            input.disabled = false;
            form.querySelector('button').disabled = false;
            input.focus();
        }
    });
})();
