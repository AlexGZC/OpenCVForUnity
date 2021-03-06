﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
    /// <summary>
    /// ArUco WebCamTexture sample.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ArUcoWebCamTextureSample : MonoBehaviour
    {
    
        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = 10;

        /// <summary>
        /// The show rejected.
        /// </summary>
        public bool showRejected = true;

        /// <summary>
        /// The estimate pose.
        /// </summary>
        public bool estimatePose = true;

        /// <summary>
        /// The length of the marker.
        /// </summary>
        public float markerLength = 100;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject ARGameObject;
        
        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera ARCamera;

        /// <summary>
        /// The cam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The dist coeffs.
        /// </summary>
        MatOfDouble distCoeffs;
                
        /// <summary>
        /// The invert Y.
        /// </summary>
        Matrix4x4 invertYM;
        
        /// <summary>
        /// The transformation m.
        /// </summary>
        Matrix4x4 transformationM;
        
        /// <summary>
        /// The invert Z.
        /// </summary>
        Matrix4x4 invertZM;
        
        /// <summary>
        /// The ar m.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The should move AR camera.
        /// </summary>
        [Tooltip("If true, only the first element of markerSettings will be processed.")]
        public bool
            shouldMoveARCamera;

        /// <summary>
        /// The identifiers.
        /// </summary>
        Mat ids ;

        /// <summary>
        /// The corners.
        /// </summary>
        List<Mat> corners;

        /// <summary>
        /// The rejected.
        /// </summary>
        List<Mat> rejected;

        /// <summary>
        /// The rvecs.
        /// </summary>
        Mat rvecs;

        /// <summary>
        /// The tvecs.
        /// </summary>
        Mat tvecs;

        /// <summary>
        /// The rot mat.
        /// </summary>
        Mat rotMat;

        /// <summary>
        /// The detector parameters.
        /// </summary>
        DetectorParameters detectorParams;

        /// <summary>
        /// The dictionary.
        /// </summary>
        Dictionary dictionary;
    

        // Use this for initialization
        void Start ()
        {
//                      Utils.setDebugMode (true);

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Init ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInited");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            colors = new Color32[webCamTextureMat.cols () * webCamTextureMat.rows ()];
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);


            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);

            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = 0;
            float height = 0;
                                    

            width = gameObject.transform.localScale.x;
            height = gameObject.transform.localScale.y;
                                    
            float imageScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageScale = (float)Screen.height / (float)Screen.width;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            rgbMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
            ids = new Mat ();
            corners = new List<Mat> ();
            rejected = new List<Mat> ();
            rvecs = new Mat ();
            tvecs = new Mat ();
            rotMat = new Mat (3, 3, CvType.CV_64FC1);


            //set cameraparam
            int max_d = (int)Mathf.Max (webCamTextureMat.rows (), webCamTextureMat.cols ());
            camMatrix = new Mat (3, 3, CvType.CV_64FC1);
            camMatrix.put (0, 0, max_d);
            camMatrix.put (0, 1, 0);
            camMatrix.put (0, 2, webCamTextureMat.cols () / 2.0f);
            camMatrix.put (1, 0, 0);
            camMatrix.put (1, 1, max_d);
            camMatrix.put (1, 2, webCamTextureMat.rows () / 2.0f);
            camMatrix.put (2, 0, 0);
            camMatrix.put (2, 1, 0);
            camMatrix.put (2, 2, 1.0f);
            Debug.Log ("camMatrix " + camMatrix.dump ());
            
            distCoeffs = new MatOfDouble (0, 0, 0, 0);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());

            //calibration camera
            Size imageSize = new Size (webCamTextureMat.cols () * imageScale, webCamTextureMat.rows () * imageScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point ();
            double[] aspectratio = new double[1];
            
            
            Calib3d.calibrationMatrixValues (camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
            
            Debug.Log ("imageSize " + imageSize.ToString ());
            Debug.Log ("apertureWidth " + apertureWidth);
            Debug.Log ("apertureHeight " + apertureHeight);
            Debug.Log ("fovx " + fovx [0]);
            Debug.Log ("fovy " + fovy [0]);
            Debug.Log ("focalLength " + focalLength [0]);
            Debug.Log ("principalPoint " + principalPoint.ToString ());
            Debug.Log ("aspectratio " + aspectratio [0]);
            
            //Adjust Unity Camera FOV
            if (widthScale < heightScale) {
                ARCamera.fieldOfView = (float)fovx [0];
            } else {
                ARCamera.fieldOfView = (float)fovy [0];
            }


                        

            transformationM = new Matrix4x4 ();

            invertYM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, -1, 1));
            Debug.Log ("invertYM " + invertYM.ToString ());
            
            invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
            Debug.Log ("invertZM " + invertZM.ToString ());

            detectorParams = DetectorParameters.create ();
            dictionary = Aruco.getPredefinedDictionary (Aruco.DICT_6X6_250);


            //if WebCamera is frontFaceing,flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice ().isFrontFacing) {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose ();
            if (ids != null)
                ids.Dispose ();
            foreach (var item in corners) {
                item.Dispose ();
            }
            corners.Clear ();
            foreach (var item in rejected) {
                item.Dispose ();
            }
            rejected.Clear ();
            if (rvecs != null)
                rvecs.Dispose ();
            if (tvecs != null)
                tvecs.Dispose ();
            if (rotMat != null)
                rotMat.Dispose ();
        }

        // Update is called once per frame
        void Update ()
        {

            if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

                // detect markers and estimate pose
                Aruco.detectMarkers (rgbMat, dictionary, corners, ids, detectorParams, rejected);
                //          Aruco.detectMarkers (imgMat, dictionary, corners, ids);
                if (estimatePose && ids.total () > 0)
                    Aruco.estimatePoseSingleMarkers (corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);


                // draw results
                if (ids.total () > 0) {
                    Aruco.drawDetectedMarkers (rgbMat, corners, ids, new Scalar (255, 0, 0));
                    
                    if (estimatePose) {
                        for (int i = 0; i < ids.total(); i++) {
                            Aruco.drawAxis (rgbMat, camMatrix, distCoeffs, rvecs, tvecs, markerLength * 0.5f);

                            //This sample can display ARObject on only first detected marker.
                            if (i == 0) {
                                Calib3d.Rodrigues (rvecs, rotMat);
                            
                            
                                transformationM.SetRow (0, new Vector4 ((float)rotMat.get (0, 0) [0], (float)rotMat.get (0, 1) [0], (float)rotMat.get (0, 2) [0], (float)tvecs.get (0, 0) [0]));
                                transformationM.SetRow (1, new Vector4 ((float)rotMat.get (1, 0) [0], (float)rotMat.get (1, 1) [0], (float)rotMat.get (1, 2) [0], (float)tvecs.get (0, 0) [1]));
                                transformationM.SetRow (2, new Vector4 ((float)rotMat.get (2, 0) [0], (float)rotMat.get (2, 1) [0], (float)rotMat.get (2, 2) [0], (float)tvecs.get (0, 0) [2]));
                                transformationM.SetRow (3, new Vector4 (0, 0, 0, 1));

                                if (shouldMoveARCamera) {
                                    ARM = ARGameObject.transform.localToWorldMatrix * invertZM * transformationM.inverse * invertYM;
//                                                              Debug.Log ("ARM " + ARM.ToString ());
                                                                
                                    ARUtils.SetTransformFromMatrix (ARCamera.transform, ref ARM);
                                } else {
                                
                                    ARM = ARCamera.transform.localToWorldMatrix * invertYM * transformationM * invertZM;
//                                                              Debug.Log ("ARM " + ARM.ToString ());
                                
                                    ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref ARM);
                                }
                            }
                        }
                    }
                }
                
                if (showRejected && rejected.Count > 0)
                    Aruco.drawDetectedMarkers (rgbMat, rejected, new Mat (), new Scalar (0, 0, 255));


                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbMat, texture, colors);
            }

        }
    
        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            webCamTextureToMatHelper.Dispose ();

//                      Utils.setDebugMode (false);
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnitySample");
            #else
            Application.LoadLevel ("OpenCVForUnitySample");
            #endif
        }

        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
        }
    }
}