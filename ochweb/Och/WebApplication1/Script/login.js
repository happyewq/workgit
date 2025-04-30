var login = function () {
    this._member = null;
};

$.extend(login.prototype,
    {
        method1: function () {
            var result = "";
            return result;
        },

        method2: function () {
            var result = "";
            return result;
        },

        onLoginClick: function (e) {
            var userID = $("#UserID").val();
            var password = $("#Password").val();

            if (!userID || !password) {
                alert("請輸入帳號和密碼！");
                return;
            }

            $.ajax({
                url: '/Login/UserLogin', // 呼叫後端 LoginController 的 UserLogin 方法
                type: 'POST',
                data: {
                    UserID: userID,
                    Password: password
                },
                success: function (response) {
                    if (response.success) {
                        alert("登入成功！");
                        // 這邊可以跳轉頁面
                        window.location.href = "/Home/Index";
                    } else {
                        alert(response.message || "登入失敗！");
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
