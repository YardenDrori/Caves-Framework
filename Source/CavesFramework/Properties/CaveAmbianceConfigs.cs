using System.Collections.Generic;
using Verse;

namespace CavesFramework;

public class EffecterSpawnerConfig
{
  public readonly EffecterDef effecter;

  //fixed rate ignores how big the cave is
  public float? mtbHoursPerSpawn;

  //scales with cave size higher -> spawn more
  public float? spawnsPerHourPer10kEmptyCells;

  public IEnumerable<string> ConfigErrors()
  {
    //xor check
    if (mtbHoursPerSpawn.HasValue && spawnsPerHourPer10kEmptyCells.HasValue)
    {
      yield return "an effect has both mtbHoursPerSpawn and spawnsPerHourPer10kEmptyCells populated, they are mutually exclusive.";
    }
    if (!mtbHoursPerSpawn.HasValue && !spawnsPerHourPer10kEmptyCells.HasValue)
    {
      yield return "an effect doesn't specify mtbHoursPerSpawn nor spawnsPerHourPer10kEmptyCells.";
    }

    //meaningfull input check
    if (mtbHoursPerSpawn.HasValue && mtbHoursPerSpawn <= 0)
    {
      yield return "an effect's mtbHoursPerSpawn has a non positive value";
    }
    if (spawnsPerHourPer10kEmptyCells.HasValue && spawnsPerHourPer10kEmptyCells <= 0)
    {
      yield return "an effect's spawnsPerHourPer10kEmptyCells has a non positive value";
    }
    if (effecter == null)
    {
      yield return "an effect has a null def.";
    }
  }
}

public class ScreenShakeConfig
{
  public float mtbHoursPerShake = 6f;
  public float shakeAmount = 0.05f;
  public int shakeDurationTicks = 120;

  public IEnumerable<string> ConfigErrors()
  {
    if (mtbHoursPerShake <= 0)
    {
      yield return "mtbHoursPerShake has a non positive value";
    }
    if (shakeAmount <= 0)
    {
      yield return "shakeAmount has a non positive value";
    }
    if (shakeAmount > 0.2f)
    {
      yield return "shakeAmount has a value over 0.2";
    }
    if (shakeDurationTicks < 1)
    {
      yield return "shakeDurationTicks has a non positive value";
    }
  }
}

public class SoundPlayConfig
{
  public SoundDef soundDef;

  //one shots only
  public float? mtbHoursPerPlay;

  //sustainers only, leave both null for constant playing
  public float? mtbHoursToStopPlaying;
  public float? mtbHoursToStartPlaying;

  public bool IsSustained => soundDef != null && soundDef.sustain;

  public IEnumerable<string> ConfigErrors()
  {
    //every check below reads the def, bail before they can nre
    if (soundDef.NullOrUndefined())
    {
      yield return "soundDef has no value.";
      yield break;
    }

    if (IsSustained)
    {
      if (mtbHoursPerPlay.HasValue)
      {
        yield return "mtbHoursPerPlay is only valid for one shot sounds, this soundDef is a sustainer.";
      }
      if (mtbHoursToStartPlaying.HasValue != mtbHoursToStopPlaying.HasValue)
      {
        yield return "mtbHoursToStartPlaying and mtbHoursToStopPlaying must both be set or both be empty. Leave both empty for the sound to play constantly.";
      }
      if (mtbHoursToStartPlaying is <= 0)
      {
        yield return "mtbHoursToStartPlaying has a non positive value.";
      }
      if (mtbHoursToStopPlaying is <= 0)
      {
        yield return "mtbHoursToStopPlaying has a non positive value.";
      }
      yield break;
    }

    if (mtbHoursToStartPlaying.HasValue || mtbHoursToStopPlaying.HasValue)
    {
      yield return "mtbHoursToStartPlaying/mtbHoursToStopPlaying are only valid for sustainer sounds, this soundDef is a one shot.";
    }
    if (!mtbHoursPerPlay.HasValue)
    {
      yield return "mtbHoursPerPlay was not provided.";
    }
    else if (mtbHoursPerPlay.Value <= 0)
    {
      yield return "mtbHoursPerPlay has a non positive value.";
    }
  }
}
