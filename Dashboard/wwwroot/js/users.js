$(document).ready(function () {
    let currentUserId = null
    let isFormVisible = false
    function loadUsers() {
        $.get("/api/usersapi/all", function (users) {
            const tbody = $("#users-table-body")
            tbody.empty()

            users.forEach((u, index) => {
                console.log(u)
                const img = u.imageData
                    ? `<img src="data:image/jpeg;base64,${u.imageData}" width="50" />`
                    : ""
                tbody.append(`
                    <tr class="user-row" data-user='${JSON.stringify(u)}'">
                        <td>${index + 1}</td>
                        <td>${img}</td>
                        <td>${u.name}</td>
                        <td>${u.email}</td>
                        <td>${new Date(u.dateOfBirth).toLocaleDateString(u.DateOfBirth)}</td>
                    </tr>
                `)
            })

            $(".user-row").click(function () {
                const user = $(this).data("user")
                currentUserId = user.id
                $("#name").val(user.name)
                $("#email").val(user.email)
                $("#date-of-birth").val(user.dateOfBirth.split('T')[0])
                toggleView()
            })
        })
    }

    function toggleView() {
        if (isFormVisible) {
            $(".table").show()
            $(".form-wrapper").hide()
            $("#add-user-btn").text("+")
            isFormVisible = false
            $("#add-user-form")[0].reset()
            currentUserId = null
        } else {
            $(".table").hide()
            $(".form-wrapper").show()
            $("#add-user-btn").text("×")
            isFormVisible = true;
        }
    }

    $(".form-wrapper").hide()


    $("#add-user-btn").click(function () {
        toggleView()
    })

    loadUsers()

    $("#add-user-form").submit(function (e) {
        e.preventDefault();
        const formData = new FormData();
        formData.append("name", $("#name").val())
        formData.append("email", $("#email").val())
        formData.append("dateOfBirth", $("#date-of-birth").val())
        const file = $("#image")[0].files[0]
        formData.append("ImageData", file)
        console.log(file)
        $.ajax({
            url: "/api/usersapi/add",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function () {
                loadUsers();
                $("#add-user-form")[0].reset()
                toggleView()
            },
            error: function (err) {
                console.log("Error adding user: " + err.responseText)
            }
        });
    }); 


});
