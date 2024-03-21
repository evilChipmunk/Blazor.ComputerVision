 
function displayImageMatrix(tableName, serializedMatrix) {
    var imageMatrix = JSON.parse(serializedMatrix); // Deserialize the matrix
    var table = document.getElementById(tableName);
    table.innerHTML = ""; // Clear previous content

    for (var i = 0; i < imageMatrix.length; i++) {
        var row = table.insertRow();
        for (var j = 0; j < imageMatrix[i].length; j++) {
            var cell = row.insertCell();
            cell.textContent = imageMatrix[i][j];
        }
    }
}

function resetColors(tableName) {
    var table = document.getElementById(tableName);
    var rows = table.getElementsByTagName("tr");

    // Reset all cell colors to white if resetColors is true
    if (resetColors) {
        // Loop through each row
        for (var i = 0; i < rows.length; i++) {
            var cells = rows[i].getElementsByTagName("td");

            // Loop through each cell in the row
            for (var j = 0; j < cells.length; j++) {
                cells[j].style.backgroundColor = "white"; // Reset cell color to white
            }
        }
    }

}

function colorCell(tableName, rowIndex, colIndex, filterSize, resetTableColors) {
    var table = document.getElementById(tableName);
    var rows = table.getElementsByTagName("tr");

    // Reset all cell colors to white if resetColors is true
    if (resetTableColors) {
        resetColors(tableName);
    }
    

    // Calculate the range of rows and columns to color based on the filter size
    var halfFilterSize = Math.floor(filterSize / 2);
    var startRow = Math.max(0, rowIndex - halfFilterSize);
    var endRow = Math.min(rows.length - 1, rowIndex + halfFilterSize);
    var startCol = Math.max(0, colIndex - halfFilterSize);
    var endCol = Math.min(rows[0].getElementsByTagName("td").length - 1, colIndex + halfFilterSize);

    // Loop through each row within the filter range
    for (var i = startRow; i <= endRow; i++) {
        var cells = rows[i].getElementsByTagName("td");

        // Loop through each cell in the row within the filter range
        for (var j = startCol; j <= endCol; j++) {
            if (i === rowIndex && j === colIndex) {
                cells[j].style.backgroundColor = "yellow"; // Color the specified cell
            } else {
                cells[j].style.backgroundColor = "red"; // Color the neighboring cells red
            }
        }
    }
}


function drawImageOnCanvas( canvasName, pixelMatrix) {
    var canvas = document.getElementById(canvasName);
    var ctx = canvas.getContext('2d');

    var imageSize = pixelMatrix.length; // Assuming square images for simplicity
    var pixelSize = canvas.width / imageSize; // Calculate the size of each "pixel" on the canvas

    for (var y = 0; y < imageSize; y++) {
        for (var x = 0; x < imageSize; x++) {
            var pixelValue = pixelMatrix[y][x]; // Your pixel value, replace with actual logic to retrieve color
            ctx.fillStyle = `rgb(${pixelValue.r}, ${pixelValue.g}, ${pixelValue.b})`;
            ctx.fillRect(x * pixelSize, y * pixelSize, pixelSize, pixelSize);
        }
    }
}
