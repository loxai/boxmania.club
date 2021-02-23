using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputUtils
{
    public static KeyCode blockHKey;
    public static KeyCode blockVKey;
    public static KeyCode bombKey;
    public static KeyCode holdKey = KeyCode.LeftShift;
    

    public static KeyCode customLayoutKeySwitch = KeyCode.Tab;
    

    //TODO feedback forrce here instead of 
    //CUSTOM LAYOUT
    public static bool isCustomLayoutSwitchDown()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKeyDown(customLayoutKeySwitch);
#else
        return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick);
#endif
    }
    //DIRECTIONAL (joystick sets direction to hit)
    //TODO
    //BLOCK
    /*
    internal static bool isBlockVKeyPressed(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKey(blockVKey);
#else
        Vector2 vec = OVRInput.Get(rightController ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick);
        return Mathf.Abs(vec.y) > 0.5f;
#endif

    }
    internal static bool isBlockHKeyPressed(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKey(blockHKey);
#else
        Vector2 vec = OVRInput.Get(rightController ? OVRInput.Axis2D.SecondaryThumbstick : OVRInput.Axis2D.PrimaryThumbstick);
        return Mathf.Abs(vec.x) > 0.5f;
#endif

    }
     * */
    //BOMB
    /*
    internal static bool isBombKeyPressed(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKey(bombKey);
#else
        return OVRInput.Get(rightController? OVRInput.Button.Two : OVRInput.Button.Four);
#endif
    }
    internal static bool isBombKeyDown(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKeyDown(bombKey);
#else
        return OVRInput.GetDown(rightController? OVRInput.Button.Two : OVRInput.Button.Four);
#endif
    }
     * */
    //HOLD
    internal static string getHoldKeyName()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return holdKey.ToString();
#else
        return "X and A";
#endif
    }
    internal static bool isHoldKeyPressed(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKey(holdKey);
#else
        return OVRInput.Get(rightController? OVRInput.Button.One : OVRInput.Button.Three);
#endif

    }
    internal static bool isHoldKeyDown(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKeyDown(holdKey);
#else
        return OVRInput.GetDown(rightController? OVRInput.Button.One : OVRInput.Button.Three);
#endif

    }
    //DRAG
    internal static string getDragKeyName()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return holdKey.ToString();
#else
        //return OVRInput.Button.Two + " " + OVRInput.Button.Four;
        return "Y and B";
#endif
    }
    internal static bool isDragPressed(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKey(holdKey);
#else
        //return OVRInput.Get(rightController ? OVRInput.Axis1D.SecondaryHandTrigger : OVRInput.Axis1D.PrimaryHandTrigger) > 0.1f;
        //return OVRInput.Get(rightController ? OVRInput.Button.SecondaryHandTrigger : OVRInput.Button.PrimaryHandTrigger);
        return OVRInput.Get(rightController? OVRInput.Button.Two : OVRInput.Button.Four);
#endif
    }

    internal static bool isDragDown(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return Input.GetKeyDown(holdKey);
#else
        //return OVRInput.Get(rightController ? OVRInput.Axis1D.SecondaryHandTrigger : OVRInput.Axis1D.PrimaryHandTrigger) > 0.1f;
        //return OVRInput.GetDown(rightController ? OVRInput.Button.SecondaryHandTrigger : OVRInput.Button.PrimaryHandTrigger);
        return OVRInput.GetDown(rightController? OVRInput.Button.Two : OVRInput.Button.Four);
#endif
    }
//    internal static bool isDragUp(bool triggeredByRightController)
//    {
//#if UNITY_STANDALONE || UNITY_EDITOR
//        return Input.GetKeyUp(holdKey);
//#else
//        return OVRInput.GetUp(triggeredByRightController ? OVRInput.Button.SecondaryHandTrigger : OVRInput.Button.PrimaryHandTrigger);
//#endif
//    }

    internal static bool isLoopKeyPressed(bool rightController)
    {
        return isHoldKeyPressed(rightController);
    }

    internal static bool isLoopKeyDown(bool rightController)
    {
        return isHoldKeyDown(rightController);
    }

    //internal static bool isEffectAKeyPressed()
    //{
    //    return isEffectAKeyPressed(true) || isEffectAKeyPressed(false);
    //}
    internal static bool isEffectAKeyPressed(bool rightController)
    {
        return Mathf.Abs(getEffectAKeyLevel(rightController)) > 0.01f;
    }

    //internal static bool isEffectBKeyPressed()
    //{
    //    return isEffectBKeyPressed(true) || isEffectBKeyPressed(false);
    //}
    internal static bool isEffectBKeyPressed(bool rightController)
    {
        return Mathf.Abs(getEffectBKeyLevel(rightController)) > 0.01f;
    }

    //internal static float getEffectBKeyLevel()
    //{
    //    return (getEffectBKeyLevel(true) + getEffectBKeyLevel(false));
    //}
    internal static float getEffectBKeyLevel(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        float result = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
            result = -0.5f;
        if (Input.GetKey(KeyCode.RightArrow))
            result = 0.5f;
        return result;
#else
        if (!OVRInput.Get(rightController ? OVRInput.Button.SecondaryThumbstick : OVRInput.Button.PrimaryThumbstick)){
            Vector2 vec = OVRInput.Get(rightController ? OVRInput.Axis2D.SecondaryThumbstick : OVRInput.Axis2D.PrimaryThumbstick);
            return vec.x;
        }
        return 0;
#endif
    }
    //internal static float getEffectAKeyLevel(){
    //    return (getEffectAKeyLevel(true) + getEffectAKeyLevel(false));
    //}

    internal static float getEffectAKeyLevel(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        float result = 0;
        if (Input.GetKey(KeyCode.UpArrow))
            result = -0.5f;
        if (Input.GetKey(KeyCode.DownArrow))
            result = 0.5f;
        return result;
#else
        if (!OVRInput.Get(rightController ? OVRInput.Button.SecondaryThumbstick : OVRInput.Button.PrimaryThumbstick)){
            Vector2 vec = -OVRInput.Get(rightController ? OVRInput.Axis2D.SecondaryThumbstick : OVRInput.Axis2D.PrimaryThumbstick);
            return vec.y;
        }
        return 0;
#endif
    }
    //TODO cross fade from key not currently working?
    internal static float getCrossFadeKeyLevel()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        float result = 0;
        if (Input.GetKey(KeyCode.K))
            result = -0.5f;
        if (Input.GetKey(KeyCode.J))
            result = 0.5f;
        return result;
#else
        float result = 0;
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick))
        {
            Vector2 vec = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            result = vec.x;
        }else
        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick)){
            Vector2 vec = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            result = vec.x;
        }
        return result;
#endif
    }

    internal static string getDirectionalKeyName()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        return "[PC Disabled]";
#else
        return "Left and Right Thumbsticks";
#endif
    }
    const float DIRECTIONAL_DEAD_ZONE = 0.5f;
    internal static bool getDirectionalDown(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        //TODO allow arrows keys?
        return false;
#else
        return OVRInput.GetDown(rightController ? OVRInput.Button.SecondaryThumbstick : OVRInput.Button.PrimaryThumbstick);
#endif
    }
    internal static int getDirectionalPress(Vector2 vec)
    {
        int dir = -1;

        if (Mathf.Abs(vec.x) > DIRECTIONAL_DEAD_ZONE || Mathf.Abs(vec.y) > DIRECTIONAL_DEAD_ZONE)
        {
            dir = 4;//N
            if (vec.y < -DIRECTIONAL_DEAD_ZONE)
                dir = 0;//S
            
            if (vec.x > DIRECTIONAL_DEAD_ZONE)
            {
                dir = 2;
                if (vec.y > DIRECTIONAL_DEAD_ZONE)
                    dir = 3;
                if (vec.y < -DIRECTIONAL_DEAD_ZONE)
                    dir = 1;
            }
            if (vec.x < -DIRECTIONAL_DEAD_ZONE)
            {
                dir = 6;
                if (vec.y > DIRECTIONAL_DEAD_ZONE)
                    dir = 7;
                if (vec.y < -DIRECTIONAL_DEAD_ZONE)
                    dir = 5;
            }
        }
        return dir;
    }
    internal static int getDirectionalPress(bool rightController)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        //TODO allow arrows keys?
        return -1;
#else
        Vector2 vec = -OVRInput.Get(rightController ? OVRInput.Axis2D.SecondaryThumbstick : OVRInput.Axis2D.PrimaryThumbstick);
        return getDirectionalPress(vec);
#endif
    }
}
