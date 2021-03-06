﻿using Aktiv.RtAdmin.Properties;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using RutokenPkcs11Interop.HighLevelAPI;
using System.Collections.Generic;

namespace Aktiv.RtAdmin
{
    public static class DriveFormatter
    {
        public static void Format(Slot slot, string adminPin, IEnumerable<VolumeFormatInfoExtended> volumeInfos)
        {
            try
            {
                slot.FormatDrive(CKU.CKU_SO, adminPin, volumeInfos);
            }
            catch (Pkcs11Exception ex) when (ex.RV == CKR.CKR_PIN_INCORRECT)
            {
                throw new CKRException(ex.RV, Resources.IncorrectAdminPin);
            }
        }
    }
}
