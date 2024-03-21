from multiprocessing import freeze_support
import torch 
from PIL import Image
from ultralytics import YOLO 
import time
import numpy as np
 
class YoloResult:
    def __init__(self, img, originalImageName):
        self.img = img 
        self.originalImageName = originalImageName
        self.yoloItems = []

    def addItem(self, item):
        self.yoloItems.append(item)

class YoloItem:
    def __init__(self, label, confidence):
        self.label = label
        self.confidence = confidence 
 
class YoloObjectIdentifier:
    def __init__(self, devMode=True) -> None:
        self.model = None
        self.devMode = devMode
        pass

    def helloworld(self, img):
        return 'hello'

    def createModel(self, modeName='yolov8n.pt'):
        # freeze_support()
        self.model = YOLO(modeName)
        if (not self.devMode):
            self.model = self.model.eval() 

    def trainModel(self):
        results = self.model.train(data='coco128.yaml', epochs=3)
        return results
    
    def exportModel(self):
        self.model.export(format='onnx')
    
    def predict(self, image_data, original_shape):   
        try: 
            np_img = np.array(image_data, dtype=np.uint8).reshape(original_shape) 
            np_img = np_img[..., ::-1]
            results = self.model.predict(np_img, verbose=True)  
        except Exception as e:
            print(e)
            return None 


        yoloResults = []
     
        for i, r in enumerate(results): 
            im_bgr = r.plot()  # BGR-order numpy array
            im_rgb = Image.fromarray(im_bgr[..., ::-1])  # RGB-order PIL image  
            np_image = np.array(im_rgb)
            flattened_image_data = np_image.flatten().tobytes()
            yoloResult = YoloResult(flattened_image_data, r.path)
            names  = r.names
            classes = r.boxes.cls.tolist()
            confidences = r.boxes.conf.tolist() 
            items = [names[i] for i in classes]

            for i in range(len(items)):
                yoloItem = YoloItem(items[i], confidences[i])
                yoloResult.addItem(yoloItem)

            yoloResults.append(yoloResult)
        return yoloResults 