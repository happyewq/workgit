var ochm040 = function () {
    this._member = null;
};

$.extend(ochm040.prototype,
    {
        SaveUser: function () {
            var userID = $("#UserID").val();
            var password = $("#Password").val();
            var usernmc = $("#UserNMC").val();

            $.ajax({
                url: '/OchM040/SaveUser',
                type: 'POST',
                data: {
                    UserID: userID,
                    Password: password,
                    usernmc: usernmc
                },
                success: function (res) {
                    Swal.fire('成功', '新增完成', 'success').then(() => {
                        window.location.href = '/OchM040/Index';
                    });
                },
                error: function (xhr) {
                    Swal.fire('錯誤', '新增失敗，請稍後再試', 'error');
                }
            });
        },

        goAddPage: function () {
            window.location.href = '/OchM040/AddUser';
        },

        onEditClick: function (e) {
            const userID = e.currentTarget.getAttribute("data-userid");
            if (!userID) return;

            // 建立隱藏 form
            const form = document.createElement("form");
            form.method = "POST";
            form.action = "/OchM040/Edit";

            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "id"; // 對應後端 Edit 的參數
            input.value = userID;

            form.appendChild(input);
            document.body.appendChild(form);
            form.submit();
        },

        DeleteUser: function (e) {
            const userID = e.currentTarget.getAttribute("data-userid");

            Swal.fire({
                title: `確定刪除 ${userID} 嗎？`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: '刪除',
            }).then(result => {
                if (result.isConfirmed) {
                    $.post('/OchM040/DeleteUser', { userID }, function (res) {
                        if (!res.errorMessage) {
                            Swal.fire('已刪除', '', 'success').then(() => location.reload());
                        } else {
                            Swal.fire('錯誤', res.errorMessage, 'error');
                        }
                    });
                }
            });
        },
        saveEdit: function () {
            const userID = document.getElementById("UserID").value;
            const userNMC = document.getElementById("UserNMC").value;
            const password = document.getElementById("Password").value;
            const confirmPassword = document.getElementById("ConfirmPassword").value;
            const NewPassword = document.getElementById("NewPassword").value;
            

            if (NewPassword !== confirmPassword) {
                Swal.fire('錯誤', '新密碼與確認密碼不一致', 'error');
                return;
            }

            const payload = {
                UserID: userID,
                UserNMC: userNMC,
                Password: password,
                ConfirmPassword: confirmPassword,
                NewPassword: NewPassword
            };

            $.ajax({
                url: '/OchM040/SaveData',
                type: 'POST',
                data: payload,
                success: function (res) {
                    if (!res.errorMessage) {
                        Swal.fire('成功', '修改完成', 'success').then(() => {
                            window.location.href = '/OchM040/Index';
                        });
                    } else {
                        Swal.fire('錯誤', res.errorMessage, 'error');
                    }
                },
                error: function () {
                    Swal.fire('錯誤', '伺服器錯誤，請稍後再試', 'error');
                }
            });
        },
    });

$(function () {
    ochm040 = new ochm040();
});
