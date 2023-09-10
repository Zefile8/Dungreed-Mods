using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.Linq;
using System.IO;

namespace Dungreed_Autofire
{
    [BepInPlugin("com.myname.Dungreed_Autofire", "Dungreed_Autofire", "1.0.0")]
    public class Base : BaseUnityPlugin
    {
        public void Awake()
        {
            On.Character_Hand.GetInputType += Character_Hand_GetInputType;
        }

        private WeaponInputType Character_Hand_GetInputType(On.Character_Hand.orig_GetInputType orig, Character_Hand self)
        {
            if (!self.connectedData)
            {
                return WeaponInputType.CONTINUE;
            }
            if (self.connectedData.inputType == WeaponInputType.ONCE)
            {
                return WeaponInputType.CONTINUE;
            }
            return self.connectedData.inputType;
        }
    }
}