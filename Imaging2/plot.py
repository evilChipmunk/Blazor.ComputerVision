import numpy as np
import matplotlib.pyplot as plt
from matplotlib import cm 
from PIL import Image

class ImagePlot:
    def __init__(self):
        pass
    
    def plot2d(self):
        imagePath = 'Mandrill.bmp'
        image = Image.open(imagePath).convert('L')
        img = np.array(image)
        
        print('plotting')
        plt.figure(figsize=(6, 6))
        plt.imshow(img, cmap='gray')
        plt.title('2D Representation')
        plt.axis('off')  # Hide the axes
        print('about to show')
        plt.show()

    def plot3d(self):
        imagePath = 'Mandrill.bmp'
        image = Image.open(imagePath).convert('L')
        img = np.array(image)

        fig = plt.figure(figsize=(8, 6))
        ax = fig.add_subplot(111, projection='3d')

        # Create grid
        x = np.linspace(0, img.shape[1]-1, img.shape[1])
        y = np.linspace(0, img.shape[0]-1, img.shape[0])
        x, y = np.meshgrid(x, y)

        # Plot surface
        surf = ax.plot_surface(x, y, img, cmap=cm.gray, linewidth=0, antialiased=False)
        ax.set_title('3D Representation')
        ax.set_zlim(0, 255)
        ax.view_init(60, 35)  # Adjust viewing angle for better visualization
        plt.show()


pl = ImagePlot()
pl.plot2d()
pl.plot3d()

# # Load the image and convert to grayscale
# image_path = 'Mandrill.bmp'
# image = Image.open(image_path).convert('L')
# image_array = np.array(image)

# # 2D representation
# plt.figure(figsize=(6, 6))
# plt.imshow(image_array, cmap='gray')
# plt.title('2D Representation')
# plt.axis('off')  # Hide the axes
# plt.show()

# # 3D representation
# fig = plt.figure(figsize=(8, 6))
# ax = fig.add_subplot(111, projection='3d')

# # Create grid
# x = np.linspace(0, image_array.shape[1]-1, image_array.shape[1])
# y = np.linspace(0, image_array.shape[0]-1, image_array.shape[0])
# x, y = np.meshgrid(x, y)

# # Plot surface
# surf = ax.plot_surface(x, y, image_array, cmap=cm.gray, linewidth=0, antialiased=False)
# ax.set_title('3D Representation')
# ax.set_zlim(0, 255)
# ax.view_init(60, 35)  # Adjust viewing angle for better visualization
# plt.show()
