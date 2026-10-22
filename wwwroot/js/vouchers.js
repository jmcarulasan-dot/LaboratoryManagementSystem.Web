window.lmsVouchers = {
    async copyText(text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return;
        }

        const field = document.createElement('textarea');
        field.value = text;
        field.setAttribute('readonly', '');
        field.style.position = 'fixed';
        field.style.left = '-9999px';
        document.body.appendChild(field);
        field.select();
        const copied = document.execCommand('copy');
        field.remove();
        if (!copied) throw new Error('Clipboard access is unavailable in this browser.');
    },

    printSlips(vouchers) {
        const printWindow = window.open('', '_blank', 'popup,width=900,height=700');
        if (!printWindow) throw new Error('Allow pop-ups for this LMS site, then try again.');

        const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[character]));
        const slips = vouchers.map(voucher => `
            <article class="slip">
                <div class="brand">ACLC COLLEGE OF MANDAUE <span>LABORATORY ACCESS</span></div>
                <div class="eyebrow">ONE-TIME VOUCHER</div>
                <code>${escapeHtml(voucher.code)}</code>
                <div class="instructions"><strong>Use this code at a lab PC</strong><br>Enter it on the computer lock screen to start your session.</div>
                <div class="footer">Valid for one use${voucher.expiresAt && voucher.expiresAt !== 'No expiry' ? ` · Expires ${escapeHtml(voucher.expiresAt)}` : ''}</div>
            </article>`).join('');

        printWindow.document.open();
        printWindow.document.write(`<!doctype html>
            <html lang="en"><head><meta charset="utf-8"><title>LMS Voucher Slips</title>
            <style>
                @page { size: A4; margin: 12mm; }
                * { box-sizing: border-box; }
                body { margin: 0; color: #101d36; font: 14px Arial, sans-serif; }
                h1 { margin: 0 0 5px; font-size: 22px; }
                .intro { margin: 0 0 18px; color: #58657a; font-size: 12px; }
                .slips { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
                .slip { min-height: 190px; padding: 16px; border: 1px dashed #8b9ab0; border-radius: 10px; break-inside: avoid; }
                .brand { display: flex; justify-content: space-between; gap: 8px; color: #bd171d; font-size: 11px; font-weight: 700; letter-spacing: .06em; }
                .brand span { color: #71809a; font-size: 9px; }
                .eyebrow { margin-top: 22px; color: #71809a; font-size: 10px; letter-spacing: .12em; text-align: center; }
                code { display: block; margin: 8px 0 13px; color: #101d36; font: 700 21px Consolas, monospace; text-align: center; }
                .instructions { border-top: 1px solid #e2e7ef; padding-top: 10px; color: #485670; font-size: 11px; line-height: 1.5; }
                .footer { margin-top: 12px; color: #71809a; font-size: 10px; }
                @media screen { body { padding: 24px; } h1, .intro { max-width: 850px; margin-left: auto; margin-right: auto; } .slips { max-width: 850px; margin: auto; } }
            </style></head><body>
            <h1>Voucher handouts</h1>
            <p class="intro">Give each slip to one student. Codes work once and are entered at the lab PC lock screen.</p>
            <main class="slips">${slips}</main>
            <script>window.onload = () => setTimeout(() => window.print(), 250);</script>
            </body></html>`);
        printWindow.document.close();
    },

    downloadCsv(vouchers) {
        const quote = value => `"${String(value ?? '').replace(/"/g, '""')}"`;
        const csv = ['Voucher code,Expires', ...vouchers.map(voucher => `${quote(voucher.code)},${quote(voucher.expiresAt)}`)].join('\r\n');
        const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }));
        const link = document.createElement('a');
        link.href = url;
        link.download = 'lms-active-voucher-codes.csv';
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};
