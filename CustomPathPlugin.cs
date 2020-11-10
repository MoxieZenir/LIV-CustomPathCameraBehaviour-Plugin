using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class CustomPathPlugin : IPluginCameraBehaviour {
#if HIDDEN
    public string ID => "CustomPathHidePlugin";
    public string name => "Hide Custom Path";
#else
    public string ID => "CustomPathPlugin";
    public string name => "Custom Path";
#endif
    public string author => "Reality Quintupled";
    public string version => "0.4.1m1";

    public static CustomPathPlugin instance;

    public IPluginSettings settings => new EmptySettings();

    public event EventHandler ApplySettings;

    private PluginCameraHelper helper;

    private string[] paths;
    private string selectedPathFileName;
    private int pathIndex;
    private PathRenderer pathRenderer;

    private TextMeshPro previous;
    private TextMeshPro next;
    private TextMeshPro hide;
    private TextMeshPro pathNameDisplay;
    private GameObject nameObject;
    private GameObject previousObject;
    private GameObject nextObject;
    private GameObject hideObject;
    private GameObject previousButton;
    private GameObject nextButton;
    private GameObject hideButton;

    private float c;
    private float speed;
    private CameraTarget mode;
    private GameObject fixedPoint;
    private Transform target;
    private Spline spline;

    private Transform wrapper;

    public CustomPathPlugin() { }
    
    public void OnActivate(PluginCameraHelper helper) {
        if (instance == null)
            instance = this;

        this.helper = helper;

#if HIDDEN
        bool hidden = true;
        wrapper = new GameObject("CustomCameraPathHidePluginWrapper").transform;
#else
        bool hidden = false;
        wrapper = new GameObject("CustomCameraPathPluginWrapper").transform;
#endif


        if (!hidden) {
            nameObject = new GameObject("PathName");
            nameObject.transform.position = new Vector3(0.15f, 1.5f, .7f);
            nameObject.transform.localScale = new Vector3(.02f, .02f, .02f);
            nameObject.transform.RotateAround(Vector3.zero, Vector3.up, 60);
            nameObject.transform.parent = wrapper;
            pathNameDisplay = nameObject.AddComponent<TextMeshPro>();
            pathNameDisplay.fontSize = 32;
            pathNameDisplay.alignment = TextAlignmentOptions.Center;
            pathNameDisplay.text = "No paths found!";
        }

        string pathDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LIV", "CustomCameraPaths");

        if (!Directory.Exists(pathDirectory))
            return;

        string selectedPath = null;

        selectedPathFileName = Path.Combine(pathDirectory, "selectedpath");

        if (File.Exists(selectedPathFileName)) {
            using (StreamReader reader = new StreamReader(selectedPathFileName)) {
                if (!reader.EndOfStream) {
                    selectedPath = reader.ReadLine();
                }
            }
        }

        paths = Directory.GetFiles(pathDirectory, "*.path");

        if (paths.Length == 0)
            return;

        pathIndex = 0;
        if(selectedPath != null) {
            int i = 0;
            foreach (string path in paths) {
                if (path == selectedPath) {
                    pathIndex = i;
                    break;
                }
                ++i;
            }
        }

        if (!hidden) {
            previousObject = new GameObject("PreviousText");
            previousObject.transform.position = new Vector3(0.0f, 1.25f, .7f);
            previousObject.transform.localScale = new Vector3(.02f, .02f, .02f);
            previousObject.transform.RotateAround(Vector3.zero, Vector3.up, 60);
            previousObject.transform.parent = wrapper;
            previous = previousObject.AddComponent<TextMeshPro>();
            previous.fontSize = 28;
            previous.alignment = TextAlignmentOptions.Center;
            previous.text = "<<";

            previousButton = new GameObject("PreviousButton");
            previousButton.AddComponent<BoxCollider>();
            previousButton.transform.localScale = new Vector3(.04f, .04f, .01f);
            previousButton.transform.position = previousObject.transform.position;
            previousButton.transform.parent = wrapper;
            InputObject previousInput = previousButton.AddComponent<InputObject>();
            previousInput.direction = -1;
            previousInput.textMesh = previous;
            previousInput.pathPlugin = this;

            nextObject = new GameObject("NextText");
            nextObject.transform.position = new Vector3(.30f, 1.25f, .7f);
            nextObject.transform.localScale = new Vector3(.02f, .02f, .02f);
            nextObject.transform.RotateAround(Vector3.zero, Vector3.up, 60);
            nextObject.transform.parent = wrapper;
            next = nextObject.AddComponent<TextMeshPro>();
            next.fontSize = 28;
            next.alignment = TextAlignmentOptions.Center;
            next.text = ">>";

            nextButton = new GameObject("NextButton");
            nextButton.AddComponent<BoxCollider>();
            nextButton.transform.localScale = new Vector3(.05f, .05f, .01f);
            nextButton.transform.position = nextObject.transform.position;
            nextButton.transform.parent = wrapper;
            InputObject nextInput = nextButton.AddComponent<InputObject>();
            nextInput.direction = 1;
            nextInput.textMesh = next;
            nextInput.pathPlugin = this;

            hideObject = new GameObject("HideText");
            hideObject.transform.position = new Vector3(0.15f, 1.1f, .7f);
            hideObject.transform.localScale = new Vector3(.02f, .02f, .02f);
            hideObject.transform.RotateAround(Vector3.zero, Vector3.up, 60);
            hideObject.transform.parent = wrapper;
            hide = hideObject.AddComponent<TextMeshPro>();
            hide.fontSize = 28;
            hide.alignment = TextAlignmentOptions.Center;
            hide.text = "Hide UI";

            hideButton = new GameObject("HideButton");
            hideButton.AddComponent<BoxCollider>();
            hideButton.transform.localScale = new Vector3(.04f, .04f, .01f);
            hideButton.transform.position = hideObject.transform.position;
            hideButton.transform.parent = wrapper;
            InputObject hideInput = hideButton.AddComponent<InputObject>();
            hideInput.direction = 0;
            hideInput.textMesh = hide;
            hideInput.pathPlugin = this;

            if (helper.playerLeftHand.GetComponent<SphereCollider>() == null) {
                SphereCollider leftHandCollider = helper.playerLeftHand.gameObject.AddComponent<SphereCollider>();
                leftHandCollider.radius = .03f;
                leftHandCollider.isTrigger = true;
                SphereCollider rightHandCollider = helper.playerRightHand.gameObject.AddComponent<SphereCollider>();
                rightHandCollider.radius = .03f;
                rightHandCollider.isTrigger = true;
            }
        }

        GameObject pathObj = new GameObject("CameraPath");
        pathObj.transform.position = Vector3.zero;
        pathObj.transform.parent = wrapper;
        pathRenderer = pathObj.AddComponent<PathRenderer>();
        if(hidden) {
            pathRenderer.HidePath();
        }

        fixedPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fixedPoint.transform.localScale = new Vector3(.05f, .05f, .05f);
        fixedPoint.GetComponent<Renderer>().material.color = new Color(34, 139, 34);
        if(hidden) {
            fixedPoint.GetComponent<Renderer>().enabled = false;
        }

        ImportCameraPath(paths[pathIndex]);
    }

    public void OnSettingsDeserialized() {}
    
    public void OnFixedUpdate() {}
    
    public void OnUpdate() {}

    public void ChangePath(int direction) {
        pathIndex = (pathIndex + direction + paths.Length) % paths.Length;
        ImportCameraPath(paths[pathIndex]);
    }

    public void OnLateUpdate() {
        if(target == null && pathNameDisplay != null) {
            pathNameDisplay.text = "Error: Camera target missing!";
            return;
        }
        Vector3 newPos = spline.PointAtTime(Time.time * speed, c);
        helper.UpdateCameraPose(newPos, Quaternion.LookRotation(target.position - newPos, Vector3.up));
    }

    public void ImportCameraPath(string filePath) {
        string pathName = new FileInfo(filePath).Name.Split('.')[0];
        if(pathNameDisplay != null) {
            pathNameDisplay.text = pathName;
        }

        using (StreamWriter writer = new StreamWriter(selectedPathFileName)) {
            writer.WriteLine(filePath);
        }

        List<Vector3> points = new List<Vector3>();
        using (StreamReader reader = new StreamReader(filePath)) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                if (line.StartsWith(";")) {
                    string[] pair = line.TrimStart(';').Split(':');
                    switch (pair[0]) {
                        case "c":
                            c = float.Parse(pair[1]);
                            break;
                        case "speed":
                            speed = float.Parse(pair[1]);
                            break;
                        case "mode":
                            mode = (CameraTarget)Enum.Parse(typeof(CameraTarget), pair[1]);
                            break;
                        case "point":
                            float[] coords = pair[1].Split(',').ToList().ConvertAll<float>(c => float.Parse(c)).ToArray();
                            fixedPoint.transform.position = new Vector3(coords[0], coords[1], coords[2]);
                            break;

                    }
                } else {
                    float[] coords = line.Split(',').ToList().ConvertAll<float>(c => float.Parse(c)).ToArray();
                    points.Add(new Vector3(coords[0], coords[1], coords[2]));
                }
            }
        }

        spline = new Spline(points);
        pathRenderer.AddPoints(points);
        pathRenderer.RenderSpline(spline, c);

        switch (mode) {
            case CameraTarget.Head:
                target = helper.playerHead;
                fixedPoint.SetActive(false);
                break;
            case CameraTarget.Fixed_Point:
                target = fixedPoint.transform;
                fixedPoint.SetActive(true);
                break;
        }
    }
    
    public void OnDeactivate() {
        if (wrapper != null)
            Object.Destroy(wrapper.gameObject);
    }
    
    public void OnDestroy() {}

    public void HideUI()
    {
        pathRenderer.HidePath();

        //fixedPoint.GetComponent<MeshRenderer>().enabled = false;
        //previousButton.GetComponent<MeshRenderer>().enabled = false;
        //nextButton.GetComponent<MeshRenderer>().enabled = false;
        //hideButton.GetComponent<MeshRenderer>().enabled = false;
        
        fixedPoint.SetActive(false);

        nameObject.SetActive(false);

        previousButton.SetActive(false);
        nextButton.SetActive(false);
        hideButton.SetActive(false);

        previousObject.SetActive(false);
        nextObject.SetActive(false);
        hideObject.SetActive(false);
    }
}
// There be no settings soooo...
public class EmptySettings : IPluginSettings { }

public enum CameraTarget {
    Head = 0,
    Fixed_Point = 1
}