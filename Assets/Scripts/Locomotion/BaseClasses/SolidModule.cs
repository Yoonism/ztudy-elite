using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolidModule : MonoBehaviour
{
     public enum SolidMaterial
     {
          Wall = 0,
          Destructable,
          Actor
     }

     public SolidMaterial solidMaterial = SolidMaterial.Wall;
}
