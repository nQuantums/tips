
# Program To Read video 
# and Extract Frames 
import cv2 

# Function to extract frames 
def FrameCapture(path1, path2): 
	
	# Path to video file 
	cap1 = cv2.VideoCapture(path1) 
	cap2 = cv2.VideoCapture(path2) 


	# Used as counter variable 
	frame_position = 14000

	# checks whether frames were extracted 
	success = 1

	while success: 
		cap1.set(cv2.CAP_PROP_POS_FRAMES, frame_position)
		# cap2.set(cv2.CAP_PROP_POS_FRAMES, frame_position)

		# cap1 object calls read 
		# function extract frames 
		success, image = cap1.read()
		# Saves the frames with frame-count 
		cv2.imshow("darksouls1", image)

		# success, image = cap2.read()
		# # Saves the frames with frame-count 
		# cv2.imshow("darksouls2", image)

		print(frame_position)

		if cv2.waitKey(1000) & 0xFF == ord('q'):
			break
		frame_position += 1

# Driver Code 
if __name__ == '__main__': 

	# Calling the function 
	FrameCapture("c:/work/mhw.webm", "c:/work/DarkSouls3Full.mp4") 
