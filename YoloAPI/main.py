from fastapi import FastAPI
from fastapi.responses import JSONResponse
from YoloObjectIdentifier import YoloObjectIdentifier  # Ensure this matches your script's structure
import numpy as np
from pydantic import BaseModel
from typing import List
import base64

class ImageDataRequest(BaseModel):
    data: str  # Base64-encoded string of the image data
    shape: List[int]

app = FastAPI()

# Initialize and load the model
yolo_identifier = YoloObjectIdentifier()


@app.on_event("startup")
async def load_model():
    yolo_identifier.createModel()


@app.post("/predict/")
async def make_prediction(request: ImageDataRequest):
    try:
        # Decode the base64 string to a byte array
        byte_data = base64.b64decode(request.data)
        
        # Convert byte data to numpy array for processing
        np_image = np.frombuffer(byte_data, dtype=np.uint8).reshape(request.shape)
        
         
        results = yolo_identifier.predict(np_image, request.shape)
 
        # Return the prediction results 
        response = [{"img": base64.b64encode(result.img).decode('utf-8'),
                    "originalImageName": result.originalImageName, 
                     "items": [{"label": item.label, "confidence": item.confidence} for item in result.yoloItems]} 
                    for result in results]
        
        return JSONResponse(content={"results": response})
    except Exception as e:
        return JSONResponse(status_code=500, content={"message": "Error processing the image", "error": str(e)})
 
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)
