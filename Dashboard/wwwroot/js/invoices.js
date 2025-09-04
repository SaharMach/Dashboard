$(document).ready(function () {
    $("#invoice-upload-form").on("submit", function (e) {
        e.preventDefault();

        var formData = new FormData();
        var fileInput = $("#invoiceFile")[0].files[0];
        formData.append("invoiceFile", fileInput);

        $.ajax({
            url: "/api/Invoicesapi/upload",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            dataType: "json",
            beforeSend: function () {
                $("#invoice-result").html(`
                    <div class="text-center my-4">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">טוען...</span>
                        </div>
                        <p class="mt-2">מעבד את הנתונים, אנא המתן...</p>
                    </div>
                `);
            },
            success: function (response) {
                renderInvoice(response);
                renderEmailPreview(response);
            },
            error: function (xhr, status, error) {
                $("#invoice-result").html('<div class="alert alert-danger">שגיאה: ' + xhr.responseText + '</div>');
            }
        });
    });

    function renderInvoice(invoice) {
        var html = `
            <div class="card mb-4 shadow-sm">
                <div class="card-header bg-primary text-white">פרטי חשבונית</div>
                <div class="card-body">
                    <div class="row mb-2">
                        <div class="col-md-6"><strong>ספק:</strong> ${invoice.ספק}</div>
                        <div class="col-md-6"><strong>לקוח:</strong> ${invoice.לקוח}</div>
                    </div>
                    <div class="row mb-2">
                        <div class="col-md-6"><strong>ח.פ ספק:</strong> ${invoice['ח.פ ספק']}</div>
                        <div class="col-md-6"><strong>ח.פ לקוח:</strong> ${invoice['ח.פ לקוח']}</div>
                    </div>
                    <div class="row mb-2">
                        <div class="col-md-6"><strong>תאריך חשבונית:</strong> ${invoice['תאריך חשבונית']}</div>
                    </div>
                </div>
            </div>

            <div class="card mb-4 shadow-sm">
                <div class="card-header bg-primary text-white">פריטים</div>
                <div class="card-body p-0">
                    <table class="table table-striped mb-0">
                        <thead class="table-light">
                            <tr>
                                <th>תיאור</th>
                                <th>כמות</th>
                                <th>מחיר יחידה</th>
                                <th>סה״כ</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${invoice.פריטים.map(item => `
                                <tr>
                                    <td>${item.תיאור}</td>
                                    <td>${item.כמות}</td>
                                    <td>${item['מחיר יחידה']}</td>
                                    <td>${item.סהכ}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>

            <div class="card mb-4 shadow-sm">
                <div class="card-header bg-primary text-white">סה״כ</div>
                <div class="card-body">
                    <p><strong>סה״כ לפני מע״מ:</strong> ${invoice['סה״כ לפני מע״מ']}</p>
                    <p><strong>סה״כ כולל מע״מ:</strong> ${invoice['סה״כ כולל מע״מ']}</p>
                </div>
            </div>`;
        $("#invoice-result").html(html);
    }

    function renderEmailPreview(invoice) {
        var html = `
            <div class="card mb-4 shadow-sm">
                <div class="card-header bg-primary text-white">שליחת מייל</div>
                <div class="card-body">
                    <div class="mb-2">
                        <label for="emailTo" class="form-label">כתובת מייל:</label>
                        <input type="email" id="emailTo" class="form-control" placeholder="example@mail.com">
                    </div>
                    <div class="mb-2">
                        <label class="form-label">תוכן המייל:</label>
                       <textarea class="form-control" id="emailBody" rows="7">${[
                `ספק: ${invoice.ספק}`,
                `לקוח: ${invoice.לקוח}`,
                `ח.פ ספק: ${invoice['ח.פ ספק']}`,
                `ח.פ לקוח: ${invoice['ח.פ לקוח']}`,
                `תאריך חשבונית: ${invoice['תאריך חשבונית']}`,
                `סה״כ לפני מע״מ: ${invoice['סה״כ לפני מע״מ']}`,
                `סה״כ כולל מע״מ: ${invoice['סה״כ כולל מע״מ']}`
            ].join('\n')}</textarea>
                    </div>
                    <button id="sendEmailBtn" class="btn btn-primary">שלח מייל</button>
                    <div id="emailStatus" class="mt-2"></div>
                </div>
            </div>`;

        $("#invoice-result").append(html);

        $("#sendEmailBtn").on("click", function () {
            var emailData = {
                to: $("#emailTo").val(),
                body: $("#emailBody").val()
            };

            var fileInput = $("#invoiceFile")[0].files[0];
            var formData = new FormData();
            formData.append("invoiceFile", fileInput);
            formData.append("to", emailData.to);
            formData.append("body", emailData.body);

            $.ajax({
                url: "/api/Invoicesapi/send-email",
                type: "POST",
                data: formData,
                contentType: false,
                processData: false,
                success: function () {
                    $("#emailStatus").html('<div class="alert alert-success">המייל נשלח בהצלחה!</div>');
                },
                error: function (xhr) {
                    $("#emailStatus").html('<div class="alert alert-danger">שגיאה בשליחת המייל: ' + xhr.responseText + '</div>');
                }
            });
        });
    }
});
