using System;

namespace LivingTomorrow.CMSApi
{
    public static class Consts
    {
        public enum Languages
        {
            auto,
            nl,
            en,
            fr,
            de
        }

        public enum AuthType
        {
            client = 0,
            gameServer = 2
        }

        public const string TimelineVersion = "2.0";

        public static string DemoId {
            get
            {
                if (string.IsNullOrEmpty(LivingTomorrowGameManager.Instance.demoSettings.DemoID))
                {
                    throw new ArgumentNullException("DemoID not set.");
                }
                return LivingTomorrowGameManager.Instance.demoSettings.DemoID;
            }
        }

        public static string TourBackendURL
        {
            get
            {
                if (string.IsNullOrEmpty(LivingTomorrowGameManager.Instance.demoSettings.TourBackendURL))
                {
                    throw new ArgumentNullException("TourBackendURL not set.");
                }
                return LivingTomorrowGameManager.Instance.demoSettings.TourBackendURL;
            }
        }

        public static AuthType CurrentAuthType 
        { 
            get
            {
                return LivingTomorrowGameManager.Instance.demoSettings.CurrentAuthType;
            } 
        }

        public static string GetUsersURI 
        { get 
            {
                return $"{TourBackendURL}/api/Demos/{DemoId}/getUsers"; 
            }
        }
        public static string GetDemoMetaDataURI
        {
            get
            {
                return $"{TourBackendURL}/api/Demos/{DemoId}/getMetadata";
            }
        }
        public static string PostGameConfigURI
        {
            get
            {
                return $"{TourBackendURL}/api/ConfigTemplates";
            }
        }

        public static string GetGameConfigURI
        {
            get
            {
                return $"{TourBackendURL}/api/Configs/";
            }
        }

        public static string GetDeviceInfoURI
        {
            get
            {
                return $"{TourBackendURL}/api/HeadsetMappings/";
            }
        }

        public static string PostDeviceInfoURI
        {
            get
            {
                return $"{TourBackendURL}/api/HeadsetMappings/";
            }
        }
        public static string MediaItemsURI
        {
            get
            {
                if (string.IsNullOrEmpty(LivingTomorrowGameManager.Instance.demoSettings.CmsURL))
                {
                    throw new ArgumentNullException("CmsURL is not set.");
                }
                return $"{LivingTomorrowGameManager.Instance.demoSettings.CmsURL}/api/media-item/get";
            }
        }

        public const string DefaultTimelineConfigString = @"{
        ""Quizzes"": [],
        ""EndDemos"": [
            {
                ""Delay"": ""00:00:00.000"",
                ""ItemID"": 650,
                ""StartOn"": ""After"",
                ""DependsOn"": 910,
                ""Conditional"": null,
                ""Description"": ""End"",
                ""PartnerDemoName"": null
            }
        ],
        ""UnityDemos"": [
            {
                ""Delay"": ""00:00:00.000"",
                ""ItemID"": 910,
                ""StartOn"": ""After"",
                ""DependsOn"": null,
                ""Conditional"": null,
                ""Description"": ""Unity Demo"",
                ""PartnerDemoName"": null
            }
        ],
        ""CMSPlayItems"": [],
        ""PartnerDemos"": [],
        ""UserAppCommands"": [],
        ""ShutDownTimeline"": [
            {
                ""EndDemos"": [
                    {
                        ""Delay"": ""00:00:00.000"",
                        ""ItemID"": 609,
                        ""StartOn"": ""After"",
                        ""DependsOn"": null,
                        ""Conditional"": null,
                        ""Description"": ""End"",
                        ""PartnerDemoName"": null
                    }
                ],
                ""LTSignages"": [],
                ""FieldControlsCommands"": []
            }
        ],
        ""UserAppFeedbacks"": [],
        ""FieldControlsCommands"": []
    }";
}
}