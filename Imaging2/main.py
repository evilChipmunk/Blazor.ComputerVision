import os 
import cv2
import numpy as np  
import time


def fun():
    return 'hi'
 
class Rectangle:
    def __init__(self, x, y, w, h):
        self.x = x
        self.y = y
        self.w = w
        self.h = h


class ParticleFilter(object):
    """A particle filter tracker.

    Encapsulating state, initialization and update methods. Refer to
    the method run_particle_filter( ) in experiment.py to understand
    how this class and methods work.
    """

    def __init__(self, num_particles, sigma_exp, sigma_dyn, template_rect):
        """Initializes the particle filter object.

        The main components of your particle filter should at least be:
        - self.particles (numpy.array): Here you will store your particles.
                                        This should be a N x 2 array where
                                        N = self.num_particles. This component
                                        is used by the autograder so make sure
                                        you define it appropriately.
                                        Make sure you use (x, y)
        - self.weights (numpy.array): Array of N weights, one for each
                                      particle.
                                      Hint: initialize them with a uniform
                                      normalized distribution (equal weight for
                                      each one). Required by the autograder.
        - self.template (numpy.array): Cropped section of the first video
                                       frame that will be used as the template
                                       to track. 

        Args:
            frame (numpy.array): color BGR uint8 image of initial video frame,
                                 values in [0, 255].
            template (numpy.array): color BGR uint8 image of patch to track,
                                    values in [0, 255].
            kwargs: keyword arguments needed by particle filter model:
                    - num_particles (int): number of particles.
                    - sigma_exp (float): sigma value used in the similarity
                                         measure.
                    - sigma_dyn (float): sigma value that can be used when
                                         adding gaussian noise to u and v.
                    - template_rect (dict): Template coordinates with x, y,
                                            width, and height values.
        """
        self.num_particles = num_particles  # required by the autograder
        self.sigma_exp = sigma_exp  # required by the autograder
        self.sigma_dyn = sigma_dyn  # required by the autograder
        self.template_rect = template_rect  # required by the autograder 
        self.particles = self.createInitialParticles(num_particles, template_rect) 
        self.weights = np.ones(shape = (self.num_particles)) / self.num_particles
        self.totalError = 0
        self.iteration = 0
        self.DefaultSimilarity = 1e-16
        self.set_template(None)
        
  
    def createInitialParticles(self, num_particles, template_rect):
        particles = np.zeros(shape=(num_particles, 2), dtype=int)    
        x = template_rect.x
        y = template_rect.y
        w = template_rect.w
        h = template_rect.h
        
        particles[:,0] = int(x + (w//2))
        particles[:,1] = int(y + (h//2))
  
        return particles
 
    def set_template(self, template):
        self.template = template

    def get_template(self):
        return self.template

    def set_iteration(self, iteration):
        self.iteration = iteration
    
    def get_particles(self):
        """Returns the current particles state.

        This method is used by the autograder. Do not modify this function.

        Returns:
            numpy.array: particles data structure.
        """
        return self.particles

    def set_particles(self, particles): 
        self.particles = particles

    def get_weights(self):
        """Returns the current particle filter's weights.

        This method is used by the autograder. Do not modify this function.

        Returns:
            numpy.array: weights data structure.
        """
        return self.weights
    
    def set_weights(self, weights): 
        self.weights = weights

    def get_error_metric(self, template, frame_cutout):
        """Returns the error metric used based on the similarity measure.

        Returns:
            float: similarity value.
        """
        try:
            dif = template.astype(np.float64) - frame_cutout.astype(np.float64)
            power = dif**2
 
            mse =(1 / template.size) * np.sum(power)   
            # return np.exp((-1 * mse) / (2 * (self.sigma_exp ** 2) * mse))
            return np.exp((-1 * mse) / (2 * (self.sigma_exp ** 2)))
        except:
            return self.DefaultSimilarity
 
    def resample_particles(self):
        """Returns a new set of particles

        This method does not alter self.particles.

        Use self.num_particles and self.weights to return an array of
        resampled particles based on their weights.

        See np.random.choice or np.random.multinomial.
        
        Returns:
            numpy.array: particles data structure.
        """   
        particles = self.particles.copy()
        px = particles[:, 0]
        py = particles[:, 1]
        px = np.random.choice(px, self.num_particles, True, self.weights)
        py = np.random.choice(py, self.num_particles, True, self.weights)
 
        particles[:, 0] = px
        particles[:, 1] = py
        return particles
 
    def getStartAndEndIndices(self, p, img, template):
        
        height = template.shape[0]
        width = template.shape[1]
 
        halfWidth = width // 2
        halfHeight = height // 2
    
        px = int(p[0])
        py = int(p[1])

        startHeight = py - halfHeight
        if startHeight < 0:
            startHeight = 0
        if startHeight + height > img.shape[0]: 
            startHeight = img.shape[0] - height 
        endHeight = startHeight + height
        
        startWidth = px - halfWidth
        if startWidth < 0:
            startWidth = 0
        if startWidth + width > img.shape[1]:
            startWidth = img.shape[1] - width
        endWidth = startWidth + width
        return startHeight, endHeight, startWidth, endWidth 
   
    def get_predicted_center(self):
        particles = self.get_particles()
        weights = self.get_weights()

        u_weighted_mean = 0
        v_weighted_mean = 0
        for i in range(self.num_particles):
            u_weighted_mean += particles[i, 0] * weights[i]
            v_weighted_mean += particles[i, 1] * weights[i]

        return [u_weighted_mean, v_weighted_mean]
 
  
    def logNoise(self, noise):
        print('Noise {0}'.format(noise.sum()))
        return
    
    def logIteration(self):
        print('')
        print('Iteration {0}' .format(self.iteration))
        return

    def logTime(self, start, end):
        # print('Time taken : {0}', end - start)
        return

    def logTotalError(self, totalError):
        print('Total Error : {0}', totalError)
        return
    
    def logPredictedCenter(self):
        print('predicted center {0}' .format(self.get_predicted_center()))
        return

    def logBestParticleIndices(self, startY, endY, startX, endX):
        print('Best Particle Indices {0} {1} {2} {3}' .format(startY, endY, startX, endX))
        return
    
    def logTemplateDiff(self, diff):
        print('Template Diff {0}' .format(diff))
        return
    
    def process(self, frame):
        """Processes a video frame (image) and updates the filter's state.

        Implement the particle filter in this method returning None
        (do not include a return call). This function should update the
        particles and weights data structures.

        Make sure your particle filter is able to cover the entire area of the
        image. This means you should address particles that are close to the
        image borders.

        Args:
            frame (numpy.array): color BGR uint8 image of current video frame,
                                 values in [0, 255].

        Returns:
            None.
        """
        
        # show(self.template)
        # show(frame) 

        self.set_iteration(self.iteration + 1)
        self.activeFrame = frame

        if self.iteration > 0:
            processNoise = np.random.normal(0, self.sigma_dyn, self.particles.shape).astype(np.int32) 
            self.particles = self.particles + processNoise 
        
   
        imgGray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        template = self.get_template() 
        template = cv2.cvtColor(template, cv2.COLOR_BGR2GRAY)
 
  
        self.totalError = 0
        i = 0
        start = time.time()
        for i in range(self.particles.shape[0]):
            if i == 40:
                b = 3
            p = self.particles[i] 
            startHeight, endHeight, startWidth, endWidth = self.getStartAndEndIndices(p, imgGray, template) 
            f = imgGray[startHeight:endHeight, startWidth:endWidth] 

            err = self.get_error_metric(template, f)  

            print('{0}:{1}, {2}:{3}'.format(startHeight, endHeight, startWidth, endWidth))
            print("Error {0}".format(err))
            
            cv2.imshow("frame", f)
            cv2.waitKey(1)
            
            self.weights[i] = err 
            self.totalError += err
            print('Total error {0}'.format(self.totalError))
 
         
        end = time.time()

        self.logIteration()
        print('Particles sum: {0}'.format( self.particles.sum()))
        print('Weights sum: {0}'.format( self.weights.sum())) 
        print('Template sum: {0}'.format( template.sum())) 
 
        
        self.logNoise(processNoise)
        self.logTime(start, end)
        self.logTotalError(self.totalError)

        if self.totalError == 0:
            self.weights = np.ones(shape=(self.num_particles)) / self.num_particles
        else:
            self.weights = self.weights / self.totalError

        self.particles = self.resample_particles() 
 
        self.logPredictedCenter() 
        a = 4
  
    def render(self, frame_in):
        """Visualizes current particle filter state.

        This method may not be called for all frames, so don't do any model
        updates here!

        These steps will calculate the weighted mean. The resulting values
        should represent the tracking window center point.

        In order to visualize the tracker's behavior you will need to overlay
        each successive frame with the following elements:

        - Every particle's (x, y) location in the distribution should be
          plotted by drawing a colored dot point on the image. Remember that
          this should be the center of the window, not the corner.
        - Draw the rectangle of the tracking window associated with the
          Bayesian estimate for the current location which is simply the
          weighted mean of the (x, y) of the particles.
        - Finally we need to get some sense of the standard deviation or
          spread of the distribution. First, find the distance of every
          particle to the weighted mean. Next, take the weighted sum of these
          distances and plot a circle centered at the weighted mean with this
          radius.

        This function should work for all particle filters in this problem set.

        Args:
            frame_in (numpy.array): copy of frame to render the state of the
                                    particle filter.
        """

        tHeight = self.template.shape[0] // 2
        tWidth = self.template.shape[1] // 2
 
        x_weighted_mean = 0
        y_weighted_mean = 0 
        for i in range(self.num_particles):
            x = self.particles[i, 0]
            y = self.particles[i, 1]
            x_weighted_mean += x * self.weights[i]
            y_weighted_mean += y * self.weights[i]
 
            cv2.circle(frame_in, (x , y ), 2, [0, 0, 255], -1) 
 
        point1 = (int(x_weighted_mean) - tWidth, int(y_weighted_mean) - tHeight)
        point2 = (int(x_weighted_mean) + tWidth, int(y_weighted_mean) + tHeight)
        cv2.rectangle(frame_in, point1, point2,[0, 255, 0])
         
        distance = 0 
        for i in range(self.num_particles):
            p = self.particles[i]
            dist = np.linalg.norm(p - (x_weighted_mean, y_weighted_mean))
            distance += dist * self.weights[i]  
        cv2.circle(frame_in, (int(x_weighted_mean), int(y_weighted_mean)), int(distance), [0, 255, 0]) 

  
class AppearanceModelPF(ParticleFilter):
    """A variation of particle filter tracker."""

    def __init__(self, num_particles, sigma_exp, sigma_dyn, template_rect, alpha):
        """Initializes the appearance model particle filter.

        The documentation for this class is the same as the ParticleFilter
        above. There is one element that is added called alpha which is
        explained in the problem set documentation. By calling super(...) all
        the elements used in ParticleFilter will be inherited so you do not
        have to declare them again.
        """ 
        super(AppearanceModelPF, self).__init__(num_particles, sigma_exp, sigma_dyn, template_rect) 
        self.alpha = alpha  

    def getBestParticleIndices(self, frame): 
        bestWeight = np.argwhere(self.weights == self.weights.max())[0]
        bestParticle = self.particles[bestWeight]
        x = bestParticle[0, 0]
        y = bestParticle[0, 1] 
        return self.getStartAndEndIndices(bestParticle[0], frame, self.template)

    def getWeightedMeanIndices(self, frame):

        tHeight = self.template.shape[0] // 2
        tWidth = self.template.shape[1] // 2
        
        height = self.template.shape[0]
        width = self.template.shape[1]
        dim = (width, height)


        x_weighted_mean = 0
        y_weighted_mean = 0 
        for i in range(self.num_particles):
            x = self.particles[i, 0]
            y = self.particles[i, 1]
            x_weighted_mean += x * self.weights[i]
            y_weighted_mean += y * self.weights[i]
        x = int(x_weighted_mean)
        y = int(y_weighted_mean)
        
        startY = y - tHeight
        if startY < 0:
            startY = 0
        if startY + height > frame.shape[0]:
            startY = frame.shape[0] - height
        endY = startY + height

        startX = x - tWidth
        if startX < 0:
            startX = 0
        if startX + width > frame.shape[1]:
            startX = frame.shape[1] - width
        endX = startX + width
        
        return startY, endY, startX, endX

    def update(self, frame):
         
        height = self.template.shape[0]
        width = self.template.shape[1]
        dim = (width, height)
 
        
        startY, endY, startX, endX = self.getBestParticleIndices(frame)

        self.logBestParticleIndices(startY, endY, startX, endX)
        # startY, endY, startX, endX = self.getWeightedMeanIndices(frame)
 
        newTemplate = frame[startY:endY, startX:endX]
        newTemplate = cv2.resize(newTemplate, dim, interpolation=cv2.INTER_CUBIC)
       
        newTemplate = self.alpha * newTemplate + (1.-self.alpha) * self.template
        newTemplate = newTemplate.astype(np.uint8)
 
        self.set_template(newTemplate)
        
        # self._set_template( self.template.astype(np.uint8) )

    def process(self, frame, iteration):
        """Processes a video frame (image) and updates the filter's state.

        This process is also inherited from ParticleFilter. Depending on your
        implementation, you may comment out this function and use helper
        methods that implement the "Appearance Model" procedure.

        Args:
            frame (numpy.array): color BGR uint8 image of current video frame, values in [0, 255].

        Returns:
            None.
        """

        # this script can be called from the run function (python) in which it carries some state
        # or from an outside agent where the filter has no state
        if iteration:
            self.set_iteration(iteration) 
 
        ParticleFilter.process(self, frame)
 
        if self.iteration % 1 == 0:
            self.update(frame) 
            # if self.totalError > .05:
            #     self.update(frame) 
  

class ImageProcessor(object):

    def __init__(self):
        np.random.seed(42)  #DO NOT CHANGE THIS SEED VALUE
        self.frame_out = None
        self.template = None

    
    def createFilter(self, filterName, template_rect, num_particles, sigma_mse, sigma_dyn, alpha):
        if filterName == 'ParticleFilter':
            filter = ParticleFilter(num_particles, sigma_mse, sigma_dyn, template_rect)
        elif filterName == 'AppearanceModel':
            filter = AppearanceModelPF(num_particles, sigma_mse, sigma_dyn, template_rect, alpha) 
        return filter
   
    def process(self, filter, template_rect, imagePath, showCv2, startingTemplate, iteration, particles, weights):  
        frame = cv2.imread(imagePath)

        # Extract template and initialize (one-time only)
        if startingTemplate is None:
            # print('template is being initialized!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!')
            self.template = frame[template_rect.y : template_rect.y + template_rect.h,
                                template_rect.x : template_rect.x + template_rect.w] 
        else:
            self.template = startingTemplate

        cv2.imshow('Tracking2', self.template)
        cv2.waitKey(1)   

        filter.set_template(self.template)

        if np.any(particles):
            filter.set_particles(particles)

        if np.any(weights):
            filter.set_weights(weights)
  
        # Process frame
        filter.process(frame, iteration)

        self.template = filter.get_template()
  
        self.frame_out = frame.copy()

        filter.render(self.frame_out)

        if showCv2:
            cv2.imshow('Tracking', self.frame_out)
            cv2.waitKey(1)   

    def getFrame(self):
        return self.frame_out

    def getTemplate(self):
	    return self.template 
    

    
 
def run():
    np.random.seed(42)  #DO NOT CHANGE THIS SEED VALUE
    # template_rect = {'x': 538, 'y': 377, 'w': 73, 'h': 117}
 
    template_rect = Rectangle(538, 377, 73, 117) 
  
    num_particles = 400  # Define the number of particles
    sigma_mse = 3  # Define the value of sigma for the measurement exponential equation
    sigma_dyn = 17  # Define the value of sigma for the particles movement (dynamics)
    alpha = .12  # Set a value for alpha
     
    imageFolder = 'G:\\t\\source\\repos\\evilChipmunk\\BlazorVision\\ConsoleTest\\images\\pres_debate'
    imgs_list = [f for f in os.listdir(imageFolder)
                 if f[0] != '.' and f.endswith('.jpg')]
    imgs_list.sort()
    i = 0
 
    imageProcessor = ImageProcessor()

    particles = None
    weights = None
    template = None
    filter = imageProcessor.createFilter('AppearanceModel', template_rect, num_particles, sigma_mse, sigma_dyn, alpha)
    for imgName in imgs_list:
        imgPath = os.path.join(imageFolder, imgName)
        imageProcessor.process(filter, template_rect, imgPath, True, template, i, particles, weights)
        template = imageProcessor.getTemplate()
        particles = filter.get_particles()
        weights = filter.get_weights()

 
run()