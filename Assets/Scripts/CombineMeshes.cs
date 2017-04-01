using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CombineMeshes : MonoBehaviour {

    void Start() {
        ProceduralTerrain.Current.combineQueue.EnqueueAction(Combine());
    }

    IEnumerator Combine() {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        //If there is meshes to combine
        if (meshFilters.Length > 1) {
            CombineInstance[] combine = new CombineInstance[meshFilters.Length - 1];

            int index = 0;
            for (int i = 0; i < meshFilters.Length; i++) {

                if (meshFilters[i].sharedMesh == null)
                    continue;

                combine[index].mesh = meshFilters[i].sharedMesh;
                combine[index].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
                index++;
            }

            transform.GetComponent<MeshFilter>().mesh = new Mesh();
            transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            GetComponent<Renderer>().materials = meshFilters[0].GetComponent<Renderer>().sharedMaterials;
            transform.gameObject.SetActive(true);
        }

        yield return null;
    }
}
