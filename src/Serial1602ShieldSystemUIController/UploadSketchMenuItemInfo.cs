using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class UploadSketchMenuItemInfo : BaseMenuItemInfo
    {
        public int SelectedSketchIndex = 0;
        public int SelectedBoardIndex = 0;
        public int SelectedPortIndex = 0;
        public int StepIndex = 0;

        public UploadSketchMenuItemInfo ()
        {
            Label = "Upload sketch";
            base.Key = "Upload";
        }

        public SketchInfo[] GetSketchInfoList ()
        {
            var sketchList = new List<SketchInfo> ();

            sketchList.Add (new SketchInfo ("SM Monitor", "monitor"));
            sketchList.Add (new SketchInfo ("Irrigator", "irrigator"));
            sketchList.Add (new SketchInfo ("Illuminator", "illuminator"));
            sketchList.Add (new SketchInfo ("Ventilator", "ventilator"));

            return sketchList.ToArray ();
        }

        public string[] GetBoardList ()
        {
            var boardList = new List<string> ();

            boardList.Add ("nano");
            boardList.Add ("uno");
            boardList.Add ("esp");

            return boardList.ToArray ();
        }

        public void Select ()
        {
            if (StepIndex <= 3)
                StepIndex++;
            else
                throw new NotImplementedException ();
        }

        public void Up ()
        {
            switch (StepIndex) {
            case 1: // Sketch
                if (SelectedSketchIndex < GetSketchInfoList ().Length)
                    SelectedSketchIndex++;
                else
                    SelectedSketchIndex = 0;
                break;
            case 2: // Board
                if (SelectedBoardIndex < GetBoardList ().Length)
                    SelectedBoardIndex++;
                else
                    SelectedBoardIndex = 0;
                break;
            }
        }

        public void Down ()
        {
            switch (StepIndex) {
            case 1: // Sketch
                if (SelectedSketchIndex > 0)
                    SelectedSketchIndex--;
                else
                    SelectedSketchIndex = GetBoardList ().Length - 1;
                break;
            case 2: // Board
                if (SelectedBoardIndex > 0)
                    SelectedBoardIndex--;
                else
                    SelectedBoardIndex = GetBoardList ().Length - 1;
                break;
            }
        }

        public void Reset ()
        {
            StepIndex = 0;
            SelectedSketchIndex = 0;
            SelectedBoardIndex = 0;
            SelectedPortIndex = 0;
        }
    }
}

