/* A component for storing application-wide config settings */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfiguration : MonoBehaviour {

    // NOTE: more cheats may be necessary. Please consult the project spec.
    public static bool cheat_invincibility = false;
    public static bool mitchell_bloch_superstar_mode = false; // if you're bored...

    /* Inspector Tunables */
    public int target_framerate = 60;

    void Awake()
    {
        Application.targetFrameRate = target_framerate;
    }

    void Update()
    {
        ProcessCheats();
    }

    /* Flip cheats on and off in response to user input */
    void ProcessCheats()
    {
        // Note: standardized controls may be found in project spec.
    }
}
