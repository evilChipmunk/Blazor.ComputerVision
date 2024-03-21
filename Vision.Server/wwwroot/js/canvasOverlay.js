var c1 = document.getElementById("dstCanvas"), c2 = document.getElementById("overlayCanvas");
var ctx1 = c1.getContext("2d"), ctx2 = c2.getContext("2d");

ctx2.setLineDash([5, 5]);

var origin = null;

var x = 0;
var y = 0;
var endX = 0;
var endY = 0;

window.onload = () => { ctx1.drawImage(document.getElementById("img"), 0, 0); }

c2.onmousedown = e => { origin = { x: e.offsetX, y: e.offsetY }; };

window.onmouseup = e => {
    var data = 'origin x : ' + x + ' origin y : ' + y + ' end x : ' + endX + ' end y : ' + endY;

    console.log(data);

    origin = null;
};

c2.onmousemove = e => {
    if (!!origin) {
        ctx2.strokeStyle = "#ff0000";
        ctx2.clearRect(0, 0, c2.width, c2.height);
        ctx2.beginPath();
        ctx2.rect(origin.x, origin.y, e.offsetX - origin.x, e.offsetY - origin.y);
        ctx2.stroke();

        x = origin.x;
        y = origin.y;

        endX = e.offsetX - origin.x;
        endY = e.offsetY - origin.y;
    }
};