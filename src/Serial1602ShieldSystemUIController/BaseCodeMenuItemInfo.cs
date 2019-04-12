using System;

namespace Serial1602ShieldSystemUIController
{
    public abstract class BaseCodeMenuItemInfo : BaseMenuItemInfo
    {
        public SystemMenuController Controller;

        public BaseCodeMenuItemInfo (SystemMenuController controller)
        {
            Controller = controller;
        }

        public abstract void Execute (int optionIndex);
    }
}

