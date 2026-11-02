using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fyp.Development.Recursion.Editor
{
    public static class MagicBookSceneTools
    {
        static Transform Need(string path)
        {
            var root=SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(t=>Path(t)==path);
            if(root==null)throw new InvalidOperationException("Missing scene object: "+path);
            return root;
        }
        static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
        static Transform Book(string name)
        {
            var matches=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name==name).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Expected one scene book named "+name+", found "+matches.Length);
            return matches[0];
        }

        [MenuItem("Tools/FYP/Magic Books/Audit Scene Instances")]
        public static void Audit()
        {
            var report=new StringBuilder();
            foreach(var path in new[]{"Magic Book","Ruin02MagicBook"})
            {
                var book=Book(path);report.AppendLine("BOOK "+Path(book)+" position="+book.position+" scale="+book.lossyScale);
                foreach(var collider in book.GetComponentsInChildren<Collider>(true))
                    report.AppendLine(" collider="+Path(collider.transform)+" "+collider.GetType().Name+" enabled="+collider.enabled+" trigger="+collider.isTrigger+" bounds="+collider.bounds);
                foreach(var animator in book.GetComponentsInChildren<Animator>(true))
                {
                    var clips=animator.runtimeAnimatorController==null?Array.Empty<AnimationClip>():animator.runtimeAnimatorController.animationClips;
                    report.AppendLine(" animator="+Path(animator.transform)+" controller="+(animator.runtimeAnimatorController==null?"<none>":animator.runtimeAnimatorController.name)+" clips="+string.Join(",",clips.Select(c=>c.name+" loop="+c.isLooping)));
                }
                foreach(var animation in book.GetComponentsInChildren<Animation>(true))
                    report.AppendLine(" animation="+Path(animation.transform)+" wrap="+animation.wrapMode+" clips="+string.Join(",",animation.Cast<AnimationState>().Select(s=>s.name+" wrap="+s.wrapMode)));
            }
            Debug.Log(report.ToString());
        }

        static Bounds RendererBounds(Transform book)
        {
            var renderers=book.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            if(renderers.Length==0)throw new InvalidOperationException("Book has no enabled renderer: "+Path(book));
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
        }
        static void FitCollider(Transform book)
        {
            var other=book.GetComponentsInChildren<Collider>(true).Where(c=>c.transform!=book).ToArray();
            if(other.Length!=0)throw new InvalidOperationException("Unexpected child colliders; refusing to alter them: "+Path(book));
            var collider=book.GetComponent<BoxCollider>();
            if(collider==null)collider=Undo.AddComponent<BoxCollider>(book.gameObject);
            var bounds=RendererBounds(book);var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);var max=-min;
            foreach(var x in new[]{bounds.min.x,bounds.max.x})foreach(var y in new[]{bounds.min.y,bounds.max.y})foreach(var z in new[]{bounds.min.z,bounds.max.z})
            {var local=book.InverseTransformPoint(new Vector3(x,y,z));min=Vector3.Min(min,local);max=Vector3.Max(max,local);}
            collider.center=(min+max)*.5f;
            var size=max-min;collider.size=new Vector3(size.x*.82f,size.y*.9f,size.z*.82f);
            collider.isTrigger=false;collider.enabled=true;
        }
        [MenuItem("Tools/FYP/Magic Books/Install Scene-Instance Collider and Idle")]
        public static void Install()
        {
            var ruin1=Book("Magic Book");var ruin2=Book("Ruin02MagicBook");
            Undo.IncrementCurrentGroup();var group=Undo.GetCurrentGroup();
            try
            {
                foreach(var book in new[]{ruin1,ruin2})
                {
                    FitCollider(book);
                    if(book.GetComponent<MagicBookIdleMotion>()==null)Undo.AddComponent<MagicBookIdleMotion>(book.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Verify();Debug.Log("Installed fitted non-trigger book colliders and scene-instance idle motion. Vendor prefabs, Animator Controllers and clips were not modified.");
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }
        [MenuItem("Tools/FYP/Magic Books/Verify Scene Instances")]
        public static void Verify()
        {
            foreach(var book in new[]{Book("Magic Book"),Book("Ruin02MagicBook")})
            {
                var collider=book.GetComponent<BoxCollider>();var idle=book.GetComponent<MagicBookIdleMotion>();var visual=RendererBounds(book);
                if(collider==null || !collider.enabled || collider.isTrigger || idle==null || !idle.enabled || collider.bounds.size.sqrMagnitude<=0f ||
                    collider.bounds.size.x>visual.size.x*1.1f || collider.bounds.size.z>visual.size.z*1.1f)
                    throw new InvalidOperationException("Magic Book collider/idle verification failed: "+Path(book));
            }
            Debug.Log("PASS Magic Books: both scene instances have fitted non-trigger BoxColliders and independent looping idle motion.");
        }
    }
}
