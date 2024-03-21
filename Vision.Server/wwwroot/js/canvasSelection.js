window.createSelection = {
    start: function (canvasId, dotnetHelper, originalWidth, originalHeight) {
        var canvas = document.getElementById(canvasId);
        var ctx = canvas.getContext('2d');
        var rect = {};
        var drag = false;
        var imageData = null;

        function mousedown(e) {
            var boundingRect = canvas.getBoundingClientRect();
            var x = (e.clientX - boundingRect.left) * (originalWidth / canvas.width);
            var y = (e.clientY - boundingRect.top) * (originalHeight / canvas.height);

            rect.startX = x;
            rect.startY = y;
            drag = true;

            imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        }

        function mouseup(e) {
            drag = false;
            ctx.putImageData(imageData, 0, 0);

            var boundingRect = canvas.getBoundingClientRect();
            var x = (e.clientX - boundingRect.left) * (originalWidth / canvas.width);
            var y = (e.clientY - boundingRect.top) * (originalHeight / canvas.height);

            rect.w = x - rect.startX;
            rect.h = y - rect.startY;

            dotnetHelper.invokeMethodAsync('OnSelectionMade', Math.round(rect.startX), Math.round(rect.startY), Math.round(rect.w), Math.round(rect.h));
        }

        function mousemove(e) {
            if (drag) {
                var boundingRect = canvas.getBoundingClientRect();
                var x = (e.clientX - boundingRect.left) * (originalWidth / canvas.width);
                var y = (e.clientY - boundingRect.top) * (originalHeight / canvas.height);

                rect.w = x - rect.startX;
                rect.h = y - rect.startY;
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                draw();
            }
        }

        function draw() {
            ctx.strokeStyle = 'green';
            ctx.lineWidth = 4;
            var scaledStartX = rect.startX * (canvas.width / originalWidth);
            var scaledStartY = rect.startY * (canvas.height / originalHeight);
            var scaledWidth = rect.w * (canvas.width / originalWidth);
            var scaledHeight = rect.h * (canvas.height / originalHeight);
            ctx.strokeRect(scaledStartX, scaledStartY, scaledWidth, scaledHeight);
        }

        canvas.addEventListener('mouseup', mouseup, false);
        canvas.addEventListener('mousedown', mousedown, false); 
        canvas.addEventListener('mousemove', mousemove, false);
    }
};