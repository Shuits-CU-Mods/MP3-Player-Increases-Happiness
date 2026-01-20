using System;

public class MyPatches
{
	public static void Play_MyPatch()
	{
        MP3IsPlaying = true;
        for (; ; )
        {
            if (base.gameObject && MP3IsPlaying)
            {
                this.body.happiness += 1;
                new WaitForSeconds(60f);
            }
            else
            {
                break;
            }
        }
    }
    public static void Exit_MyPatch()
    {
        MP3IsPlaying = false;
    }
}
