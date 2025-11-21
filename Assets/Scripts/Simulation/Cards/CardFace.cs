using UnityEngine;

namespace GraphTheory
{
    public class CardFace : MonoBehaviour
    {
        public enum FaceType
        {
            Front,
            Back
        }
        public FaceType faceType;
        // You can add more properties or methods related to the card face here

        
        Collider myCollider;
        private Material faceMat;
        public MeshRenderer meshRenderer;
        public NodeBehavior nodeBehavior;

        public bool isOnTop = false;
        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            myCollider = GetComponent<Collider>();
            nodeBehavior = GetComponent<NodeBehavior>();
        }

        

        public void SetFaceVisibility(bool isVisible)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isVisible;
            }
        }

        public void SetInteractivity(bool isInteractive)
        {
            if (myCollider != null)
            {
                myCollider.enabled = isInteractive;
            }
        }

        public void SetFaceMaterial(Material material)
        {
            faceMat = material;
        }

        public void PrintFace()
        {
            if (meshRenderer != null && faceMat !=null)
            {
                meshRenderer.sharedMaterial = faceMat;
            }
        }

        public void PrintBack()
        {
            if (meshRenderer != null)
            {
                // Assuming you have a predefined back material
                var backMaterial = Card_Stacks.Instance.defaultBackfaceMat;
                if(backMaterial != null)
                    meshRenderer.sharedMaterial = backMaterial;
            }
        }

        public bool IsClicked()
        {
            return Input.GetMouseButtonDown(0) && myCollider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, Mathf.Infinity);
        }
    }
}
