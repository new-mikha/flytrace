﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FlyTrace.LocationLib.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int SPOT_MaxCallsInPack {
            get {
                return ((int)(this["SPOT_MaxCallsInPack"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000")]
        public int SPOT_MinTimeFromPrevStart_Ms {
            get {
                return ((int)(this["SPOT_MinTimeFromPrevStart_Ms"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int SPOT_MinCallsGap_Ms {
            get {
                return ((int)(this["SPOT_MinCallsGap_Ms"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("150")]
        public int SPOT_SameFeedHitInterval_Seconds {
            get {
                return ((int)(this["SPOT_SameFeedHitInterval_Seconds"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://api.findmespot.com/spot-main-web/consumer/rest-api/2.0/public/feed/{0}/me" +
            "ssage.xml")]
        public string SPOT_RequestUrlTemplate {
            get {
                return ((string)(this["SPOT_RequestUrlTemplate"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://api.findmespot.com/spot-main-web/consumer/rest-api/2.0/public/feed/{0}/me" +
            "ssage.xml?start={1}")]
        public string SPOT_RequestUrlTemplateWithPage {
            get {
                return ((string)(this["SPOT_RequestUrlTemplateWithPage"]));
            }
        }
    }
}
