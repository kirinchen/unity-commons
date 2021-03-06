using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace surfm.tool {
    public static class UnityUtils {

        public static List<Transform> listChild(this Transform t) {
            List<Transform> ans = new List<Transform>();
            for (int i = 0; i < t.childCount; i++) {
                ans.Add(t.GetChild(i));
            }
            return ans;
        }

        public static T getComponentByName<T>(this GameObject mb, string name) where T : Component {
            return getComponentByName<T>(mb, c => { return c.name.Equals(name); });
        }


        public static T getComponentByName<T>(GameObject mb, Predicate<T> p) where T : Component {
            string debug = "";
            foreach (T c in mb.GetComponentsInChildren<T>(true)) {
                debug += c.name + ",";
                if (p(c)) {
                    return c;
                }
            }
            return null;
        }

        public static List<T> getComponentsByName<T>(GameObject mb, Predicate<T> p) where T : Component {
            List<T> ans = new List<T>(mb.GetComponentsInChildren<T>(true));
            return ans.FindAll(p);
        }

        public static void takeCameraShot(Camera camera, int resWidth, int resHeight, string filename) {

            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            camera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            GameObject.Destroy(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            System.IO.FileInfo file = new System.IO.FileInfo(filename);
            file.Directory.Create();
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}  exit={1} w={2} h={3} ", filename, System.IO.File.Exists(filename), resWidth, resHeight));

        }

        public static void takeCameraShot(Camera camera, string filename) {
            float resHeight = Screen.currentResolution.height;
            float resWidth = Screen.currentResolution.width;

            takeCameraShot(camera, (int)resWidth, (int)resHeight, filename);
        }

        public static Coroutine delay(this MonoBehaviour mb, System.Action a, float it) {
            return mb.StartCoroutine(_delay(a, it));
        }

        public static Coroutine loop(this MonoBehaviour mb, System.Action a, float start, float it) {
            return mb.StartCoroutine(_loop(a, start, it));
        }

        private static IEnumerator _loop(System.Action a, float start, float it) {
            yield return new WaitForSeconds(start);
            while (true) {
                a();
                yield return new WaitForSeconds(it);
            }
        }

        private static IEnumerator _delay(System.Action a, float it) {
            yield return new WaitForSeconds(it);
            a();
        }

        public static void setAsync(MonoBehaviour mb, Task task, System.Action cb) {
            mb.StartCoroutine(setAsync(task, cb));
        }

        public static IEnumerator wait(Func<bool> g, Action a) {
            while (g()) {
                yield return new WaitForSeconds(.2f);
            }
            a();
        }

        public static IEnumerator setAsync(Task task, System.Action cb) {
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) {
                throw task.Exception;
            } else {
                cb();
            }
        }

        public static IEnumerator leap<I>(I i, Predicate<I> go, Action<I> task, float gap) {
            while (go(i)) {
                task(i);
                yield return new WaitForSeconds(gap);
            }
        }


        public static Component copyComponent(Component comp, Component other) {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos) {
                if (pinfo.CanWrite) {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    } catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp;
        }

        public static Component addComponent(GameObject go, Component toAdd) {
            Component n = go.AddComponent(toAdd.GetType());
            return copyComponent(n, toAdd);
        }


        public static bool mouseRaycast(Vector3 mousePos, int layerId, out RaycastHit hit) {
            LayerMask mask = 1 << layerId;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            return (Physics.Raycast(ray, out hit, Mathf.Infinity, mask));
        }

        public static RaycastHit[] castCapsuleByCollider(Vector3 pos, CapsuleCollider col, int layerMask) {
            float disP = col.height / 2 - col.radius;
            Vector3 p1 = pos + col.center + Vector3.up * disP;
            Vector3 p2 = pos + col.center - Vector3.up * disP;
            float r = col.radius * 0.99f;
            float castD = 1f;
            return Physics.CapsuleCastAll(p1, p2, r, new Vector3(0.001f, 0.001f, 0.001f), castD, layerMask);
        }

        public static void setLayerRecursively(GameObject go, int layerNumber) {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true)) {
                trans.gameObject.layer = layerNumber;
            }
        }

        public static float measureDistance(Component a, Component b) {
            Vector3 av = a.transform.position;
            Vector3 bv = b.transform.position;
            return Vector3.Distance(av,bv);
        }


    }
}