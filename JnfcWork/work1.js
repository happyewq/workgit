
const startDate = new Date("2021-08-15");
const poeticPhrases = [
  "此時此刻，愛還在繼續",
  "在這一刻，我們正在一起",
  "愛的時間流動中",
  "愛情日記的這一頁",
  "屬於我們的現在是",
  "你出現在我的每一秒",
  "我們的故事，繼續書寫著",
  "這一刻，我們共享同一個時光"
];
const timeEl = document.getElementById("time");
function updateTimeSmooth() {
  timeEl.style.opacity = 0;
  setTimeout(() => {
    const now = new Date();
    const timeString = now.toLocaleString("zh-TW", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
    const phrase = poeticPhrases[Math.floor(Math.random() * poeticPhrases.length)];
    timeEl.textContent = `${phrase}：${timeString}`;
    timeEl.style.opacity = 1;
  }, 1000);
}
function updateLoveDuration() {
  const now = new Date();
  const diffTime = now - startDate;
  const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
  document.getElementById("loveDuration").textContent = `我們已經一起走過了 ${diffDays} 天囉 ❤️`;
}
const icons = ['💖', '💫', '🌟', '💕', '✨'];
const container = document.getElementById('falling-container');
function createFallingIcon() {
  const el = document.createElement('div');
  el.classList.add('falling');
  el.style.left = Math.random() * 100 + 'vw';
  el.style.animationDuration = 2 + Math.random() * 3 + 's';
  el.style.fontSize = 20 + Math.random() * 20 + 'px';
  el.textContent = icons[Math.floor(Math.random() * icons.length)];
  container.appendChild(el);
  setTimeout(() => container.removeChild(el), 5000);
}
document.addEventListener("keydown", function (e) {
  if (
    e.key === "F12" ||
    (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === "i") ||
    (e.ctrlKey && e.key.toLowerCase() === "u") ||
    (e.ctrlKey && e.key.toLowerCase() === "s")
  ) {
    e.preventDefault();
    return false;
  }
});
document.addEventListener("contextmenu", function (e) {
  e.preventDefault();
});
window.addEventListener("DOMContentLoaded", () => {
  const today = new Date();
  if (today.getMonth() === 3 && today.getDate() === 19) {
    const letter = document.createElement("div");
    letter.id = "love-letter";
    letter.className = "letter-overlay";
    letter.innerHTML = `
      <div class="letter">
        <div class="letter-content">
          <p>親愛的：</p>
          <p>今天是屬於我們的特別日子。<br>感謝你陪我走過每個微笑與眼淚的片段。</p>
          <p>願我們的故事，繼續在每個日常裡閃閃發光～❤️</p>
          <button onclick="document.getElementById('love-letter').style.display='none'">我知道了</button>
        </div>
      </div>`;
    document.body.appendChild(letter);
  }
});
document.querySelector(".reminder-lock").addEventListener("click", () => {
  const today = new Date();
  // 如果不是 4/19，顯示提示框
  if (!(today.getMonth() === 3 && today.getDate() === 19)) {
    document.getElementById("lock-message").style.display = "flex";
  }
});
setInterval(createFallingIcon, 500);
setInterval(updateTimeSmooth, 5000);
updateTimeSmooth();
updateLoveDuration();