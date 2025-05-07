var login = function () {
    this._member = null;
};

$.extend(login.prototype,
    {
        onLoginClick: function (e) {
            var userID = $("#UserID").val();
            var password = $("#Password").val();

            if (!userID || !password) {
                alert("請輸入帳號和密碼！");
                return;
            }
            // 顯示 loading 畫面
            Swal.fire({
                title: '登入中...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            $.ajax({
                url: '/Login/UserLogin',
                type: 'POST',
                data: {
                    UserID: userID,
                    Password: password
                },
                success: function (response) {
                    debugger;
                    if (response && !response.errorMessage) {
                        // 登入成功（沒有 ErrorMessage）
                        login._member = {
                            userID: response.userID,
                            userNMC: response.userNMC,
                            permission: response.Permission
                        };
                        localStorage.setItem('userNMC', response.userNMC);
                        localStorage.setItem('permission', response.permission);
                        Swal.fire({
                            icon: 'success',
                            title: '登入成功！',
                            text: '歡迎 ' + response.userNMC,
                            confirmButtonText: '確定'
                        }).then(() => {
                            window.location.href = "/Home/Index";
                        });

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: '登入失敗！',
                            text: "錯誤訊息：" + response.errorMessage, // 加上冒號與空格使格式一致
                            confirmButtonText: '確定'
                        }).then(() => {
                            window.location.href = "/Login/Index"; // 登入頁路徑正確就可
                        });
                    }
                },
                error: function (xhr, status, error) {
                    alert("系統錯誤，請稍後再試！");
                    console.error(xhr.responseText);
                }
            });
        }

    });

$(function () {
    login = new login();
});
