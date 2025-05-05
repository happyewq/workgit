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
                            userNMC: response.userNMC
                        };
                        localStorage.setItem('userNMC', response.userNMC);
                        Swal.fire({
                            icon: 'success',
                            title: '登入成功！',
                            text: '歡迎 ' + response.userNMC,
                            confirmButtonText: '進入系統'
                        }).then(() => {
                            window.location.href = "/OchC010/Index";
                        });

                    } else {
                        // 登入失敗，顯示錯誤訊息
                        alert(response.errorMessage || "登入失敗！");
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
