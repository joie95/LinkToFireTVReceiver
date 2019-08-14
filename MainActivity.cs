using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using Sockets.Plugin;
using String = System.String;
using Android.Net;
using Uri = Android.Net.Uri;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Media;
using static Android.Media.MediaPlayer;
using Encoding = System.Text.Encoding;

namespace LinkToFireTVReceiver
{
    [Activity(Name = "LinkToFireTVReceiver.MainActivity", Label = "@string/app_name", Theme = "@style/Theme.AppCompat.Dark.NoActionBar")]
    public class MainActivity : Activity, IOnInfoListener
    {
        //OnCreate
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            //Suche VideoView
            VideoView videoView1 = (VideoView)FindViewById(Resource.Id.videoView1);
            ProgressBar progressBar1 = (ProgressBar)FindViewById(Resource.Id.progressBar1);
            TextView textView1 = (TextView)FindViewById(Resource.Id.textView1);


            //Erzeuge MediaController
            MediaController mediaController = new MediaController(this, true);
            mediaController.SetAnchorView(videoView1);
            videoView1.SetMediaController(mediaController);
            videoView1.SetOnInfoListener(this);

            //übergebene Uri extrahieren
            Uri videoUri = this.Intent.Data;

            //Falls Uri vorhanden --> Starte Video
            if (string.IsNullOrEmpty(this.Intent.DataString) == false)
            {
                //ProgressBar anzeigen und videoView leeren
                textView1.Text = "Starte Videostream:\n" + videoUri.ToString();
                progressBar1.Visibility = ViewStates.Visible;
                videoView1.Visibility = ViewStates.Gone;
                videoView1.Visibility = ViewStates.Visible;

                //Öffne Video Stream
                videoView1.SetVideoURI(videoUri);
                videoView1.Start();
            }
            //Ansonste --> Starte Service
            else
            {
                //Überprüfe ob Service schon läuft


                //Starte Service
                Intent i = new Intent(this, typeof(MainService));
                i.AddFlags(ActivityFlags.NewTask);
                this.StartService(i);

            }

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public string GetIPAddress()
        {
            IPAddress[] adresses = Dns.GetHostAddresses(Dns.GetHostName());

            if (adresses != null && adresses[0] != null)
            {
                return adresses[0].ToString();
            }
            else
            {
                return null;
            }
        }

        //Info Listener für Buffering
        public bool OnInfo(MediaPlayer mp, [GeneratedEnum] MediaInfo what, int extra)
        {
            ProgressBar progressBar1 = (ProgressBar)FindViewById(Resource.Id.progressBar1);
            TextView textView1 = (TextView)FindViewById(Resource.Id.textView1);

            switch (what)
            {
                case Android.Media.MediaInfo.BufferingStart:
                    progressBar1.Visibility = ViewStates.Visible;
                    break;
                case Android.Media.MediaInfo.BufferingEnd:
                    progressBar1.Visibility = ViewStates.Gone;
                    textView1.Visibility = ViewStates.Gone;
                    break;
                case Android.Media.MediaInfo.VideoRenderingStart:
                    progressBar1.Visibility = ViewStates.Gone;
                    textView1.Visibility = ViewStates.Gone;
                    break;
            }

            return false;
        }

        //OnPause
        protected override void OnPause()
        {
            //Gebe Activity frei
            base.OnDestroy();
            Finish();
        }

    }

    [BroadcastReceiver (Name="LinkToFireTVReceiver.BootReceiver", Enabled = true, Exported = true, DirectBootAware = true)]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Intent i = new Intent(context, typeof(MainService));
            i.AddFlags(ActivityFlags.NewTask);
            context.StartService(i);
        }
    }

    [Service(Name = "LinkToFireTVReceiver.MainService")]
    public class MainService : IntentService
    {
        protected override async void OnHandleIntent(Intent intent)
        {
            
            
            //UDP Receiver
            var listenPort = 15000;
            var receiver = new UdpSocketReceiver();
            receiver.MessageReceived += (sender, args) =>
            {
                //Daten und Senderadresse auswerten
                var from = String.Format("{0}:{1}", args.RemoteAddress, args.RemotePort);
                var data = Encoding.UTF8.GetString(args.ByteData, 0, args.ByteData.Length);

                //Falls Link übermittelt
                if (string.IsNullOrEmpty(data) == false)
                {

                    //Starte MainActivity (mit erhaltener URI)
                    Intent i = new Intent(this, typeof(MainActivity));
                    i.SetData(Uri.Parse(data));
                    //i.AddFlags(ActivityFlags.NewTask);
                    this.StartActivity(i);

                }

            };

            //Abhören am UDP Port starten
            await receiver.StartListeningAsync(listenPort);
        }

        

    }

}

