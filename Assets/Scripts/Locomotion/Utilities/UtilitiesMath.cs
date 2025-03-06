using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilitiesMath
{
     public static Vector3 LengthDir2D(float direction, float distance)
     {
          float radianAngle = direction * Mathf.PI / 180f;
          return new Vector3(distance * Mathf.Cos(radianAngle), distance * Mathf.Sin(radianAngle), 0f);
     }

     public static Vector3 Dir2D(float direction)
     {
          float radianAngle = direction * Mathf.PI / 180f;
          return new Vector3(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle), 0f);
     }

     public static int ApproximateSign(float v)
     {
          if (Mathf.Approximately(v, 0)) return 0;
          else if (v > 0) return 1;
          else return -1;
     }

     public static Vector3 Reflect2D(Vector2 normal, Vector3 eulerAngles)
     {
          // DIRTY solution
          float inAngle;
          if (normal.x > 0f)
          {
               if (normal.y > 0f) inAngle = 45f;
               else if (normal.y < 0f) inAngle = 315f;
               else inAngle = 0f;
          }
          else if (normal.x < 0f)
          {
               if (normal.y > 0f) inAngle = 135f;
               else if (normal.y < 0f) inAngle = 225f;
               else inAngle = 180f;
          }
          else
          {
               if (normal.y > 0f) inAngle = 90f;
               else if (normal.y < 0f) inAngle = 270f;
               else inAngle = 0f;
          }

          inAngle += 90f;

          //float inAngle = Vector2.SignedAngle(Vector2.zero, normal);
          float outAngle = inAngle + (inAngle - eulerAngles.z);
          //Debug.Log("normal : " + normal + ", inAngle : " + inAngle + ", outAngle : " + outAngle);

          return new Vector3(0f, 0f, outAngle);
     }
}
