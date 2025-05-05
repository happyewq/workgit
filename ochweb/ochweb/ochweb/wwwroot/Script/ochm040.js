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
    });

$(function () {
    ochm040 = new ochm040();
});
