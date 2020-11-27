using System;
namespace MUCGO_zero_CS
{
    using static MD;
    using static LARGE_MD;
    using static Stone;
    using static HashInfo;
    using static ConstValues;
    using static Board;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;

    public static class PatternHash
    {
        public static ulong[,] hash_bit = new ulong[BOARD_MAX, (int)HASH_KO + 1];
        public static ulong[] shape_bit = new ulong[BOARD_MAX];
        private static string HashBitpath = Utils.GetExpFilePath() + "HashBit.bin";
        private static string ShapeBitpath = Utils.GetExpFilePath() + "ShapeBit.bin";

        //private static uint used;
        //private static int oldest_move;
        private static readonly ulong[,] random_bitstrings = new ulong[BIT_MAX, (int)S_MAX]{
            { 0xc96d191cf6f6aea6LU, 0x401f7ac78bc80f1cLU, 0xb5ee8cb6abe457f8LU, 0xf258d22d4db91392LU },
            { 0x04eef2b4b5d860ccLU, 0x67a7aabe10d172d6LU, 0x40565d50e72b4021LU, 0x05d07b7d1e8de386LU },
            { 0x8548dea130821accLU, 0x583c502c832e0a3aLU, 0x4631aede2e67ffd1LU, 0x8f9fccba4388a61fLU },
            { 0x23d9a035f5e09570LU, 0x8b3a26b7aa4bcecbLU, 0x859c449a06e0302cLU, 0xdb696ab700feb090LU },
            { 0x7ff1366399d92b12LU, 0x6b5bd57a3c9113efLU, 0xbe892b0c53e40d3dLU, 0x3fc97b87bed94159LU },
            { 0x3d413b8d11b4cce2LU, 0x51efc5d2498d7506LU, 0xe916957641c27421LU, 0x2a327e8f39fc19a6LU },
            { 0x3edb3bfe2f0b6337LU, 0x32c51436b7c00275LU, 0xb744bed2696ed37eLU, 0xf7c35c861856282aLU },
            { 0xc4f978fb19ffb724LU, 0x14a93ca1d9bcea61LU, 0x75bda2d6bffcfca4LU, 0x41dbe94941a43d12LU },
            { 0xc6ec7495ac0e00fdLU, 0x957955653083196eLU, 0xf346de027ca95d44LU, 0x702751d1bb724213LU },
            { 0x528184b1277f75feLU, 0x884bb2027e9ac7b0LU, 0x41a0bc6dd5c28762LU, 0x0ba88011cd101288LU },
            { 0x814621bd927e0dacLU, 0xb23cb1552b043b6eLU, 0x175a1fed9bbda880LU, 0xe838ff59b1c9d964LU },
            { 0x07ea06b48fca72acLU, 0x26ebdcf08553011aLU, 0xfb44ea3c3a45cf1cLU, 0x9ed34d63df99a685LU },
            { 0x4c7bf671eaea7207LU, 0x5c7fc5fc683a1085LU, 0x7b20c584708499b9LU, 0x4c3fb0ceb4adb6b9LU },
            { 0x4902095a15d7f3d2LU, 0xec97f42c55bc9f40LU, 0xa0ffc0f9681bb9acLU, 0xc149bd468ac1ac86LU },
            { 0xb6c1a68207ba2fc9LU, 0xb906a73e05a92c74LU, 0x11e0d6ebd61d941dLU, 0x7ca12fb5b05b5c4dLU },
            { 0x16bf95defa2cd170LU, 0xc27697252e02cb81LU, 0x6c7f49bf802c66f5LU, 0x98d3daaa3b2e8562LU },
            { 0x161f5fc4ba37f6d7LU, 0x45e0c63e93fc6383LU, 0x9fb1dbfbc95c83a0LU, 0x38ddd8a535d2cbbdLU },
            { 0x39b6f08daf36ca87LU, 0x6f23d32e2a0fd7faLU, 0xfcc027348974b455LU, 0x360369eda9c0e07dLU },
            { 0xda6c4763c2c466d7LU, 0x48bbb7a741e6ddd9LU, 0xd61c0c76deb4818cLU, 0x5de152345f136375LU },
            { 0xef65d2fcbb279cfdLU, 0xdc22b9f9f9d7538dLU, 0x7dac563216d61e70LU, 0x05a6f16b79bbd6e9LU },
            { 0x5cb3b670ae90be6cLU, 0xbc87a781b47462ceLU, 0x84f579568a8972c8LU, 0x6c469ad3cba9b91aLU },
            { 0x076eb3891fd21cabLU, 0xe8c41087c07c91fcLU, 0x1cb7cd1dfbdab648LU, 0xfaec2f3c1e29110dLU },
            { 0xb0158aacd4dca9f9LU, 0x7cc1b5019ea1196dLU, 0xbc647d48e5e2aeb0LU, 0x96b30966f70500d8LU },
            { 0x87489ee810f7daa5LU, 0x74a51eba09dd373dLU, 0xd40bb2b0a7ca242dLU, 0xded20384ba4b0368LU },
            { 0x7dd248ab68b9df14LU, 0xf83326963d78833dLU, 0xe38821faf65bb505LU, 0x23654ff720304706LU },
            { 0x6fc1c8b51eec90b2LU, 0x580a8a7e936a997fLU, 0x1e7207fe6315d685LU, 0x8c59c6afcbfab7bfLU },
            { 0xc24f82b980d1fa2eLU, 0x084b779ccc9fbe44LU, 0x1a02f04511f6064eLU, 0x9640ec87ea1bee8aLU },
            { 0xb1ee0052dd55d069LU, 0xcab4f30bb95c5561LU, 0xd998babcaf69019fLU, 0xe0126bea2556ccd2LU },
            { 0x9b016f17c8800310LU, 0xf41cc5d147950f43LU, 0xfda9511773320334LU, 0xddf85a4c56345e4dLU },
            { 0xa4e47a8efae8deabLU, 0x9acaa313e6ded943LU, 0xe9a600be8f5c822bLU, 0x778d332a7e54ab53LU },
            { 0x1442a265cefe20caLU, 0xe78262e6b329807cLU, 0xd3ccfa96fed4ad17LU, 0x25b6315bb4e3d4f1LU },
            { 0xcea2b7e820395a1fLU, 0xab3b169e3f7ba6baLU, 0x237e6923d4000b08LU, 0xac1e02df1e10ef6fLU },
            { 0xd519dc015ebf61b2LU, 0xf4f51187fe96b080LU, 0xa137326e14771e17LU, 0x5b10d4a4c1fc81eaLU },
            { 0x52bed44bc6ec0a60LU, 0x10359cffb84288ceLU, 0x47d17b92cd7647a9LU, 0x41c9bafdb9158765LU },
            { 0x16676aa636f40c88LU, 0x12d8aefdff93ad5cLU, 0x19c55cbab761fc6eLU, 0x2174ee4468bdd89fLU },
            { 0xa0bd26f5eddaac55LU, 0x4fdda840f2bea00dLU, 0xf387cba277ee3737LU, 0xf90bba5c10dac7b4LU },
            { 0x33a43afbda5aeebeLU, 0xb9e3019d9af169bbLU, 0xad210ac8d15bbd2bLU, 0x9132a5599c996d32LU },
            { 0xb7e64eb925c34b07LU, 0x35cb859f0469f3c8LU, 0xbf1f44d40cbdfdaeLU, 0xbfbabeaa1611b567LU },
            { 0xe4ea67d4c915e61aLU, 0x1debfa223ca7efe1LU, 0xa77dfc79c3a3071aLU, 0x06cc239429a34614LU },
            { 0x4927012902f7e84cLU, 0x9ca15a0aff31237fLU, 0x5d9e9bc902c99ca8LU, 0x47fa9818255561ffLU },
            { 0xb613301ca773d9f1LU, 0xde64d791fb9ac4faLU, 0x1f5ac2193e8e6749LU, 0xe312b85c388acffbLU },
            { 0x986b17a971a64ff9LU, 0xcb8b41a1609c47bbLU, 0x9132359c66f27446LU, 0xfd13d5b1693465e5LU },
            { 0xf676c5b9c8c31decLU, 0x819c9d4648bde72eLU, 0xcb1b9807f2e17075LU, 0xb833da21219453aeLU },
            { 0x66f5c5f44fb6895fLU, 0x1db2622ebc8a5156LU, 0xd4d55c5a8d8e65c8LU, 0x57518131d59044b5LU },
            { 0xcfda297096d43d12LU, 0x3c92c59d9f4f4fc7LU, 0xef253867322ed69dLU, 0x75466261f580f644LU },
            { 0xda5501f76531dfafLU, 0xbff23daff1ecf103LU, 0x5ea264d24cafa620LU, 0xa4f6e95085e2c1d3LU },
            { 0x96fd21923d8280b4LU, 0xd7e000660c4e449dLU, 0x0175f4ea08c6d68fLU, 0x2fc41e957fb4d4c4LU },
            { 0x4c103d0c50171bc7LU, 0x56b4530e5704ae62LU, 0xb9d88e9704345821LU, 0xfe9bba04dff384a1LU },
            { 0xe6e0124e32eda8e3LU, 0xc45bfbf985540db8LU, 0x20f9dbcc42ded8c7LU, 0x47814256f39a4658LU },
            { 0x20dcfe42bcb14929LU, 0xe38adfbdc8aaba12LU, 0xce488f3a3480ba0dLU, 0x669aa0a29e8fba7cLU },
            { 0x87014f5f7986e0f5LU, 0x4c13ab920adf86f3LU, 0xeaec363831ef859dLU, 0xd012ad6ad0766d3eLU },
            { 0x849098d9f6e9e379LU, 0x99a456e8a46cf927LU, 0xd5756ecf52fa0945LU, 0x7a595501987485daLU },
            { 0x54440bc1354ae014LU, 0x979dad1d15e065ddLU, 0xd37e09f9234fd36fLU, 0x778f38e1b1ff715cLU },
            { 0x443d82e64256a243LU, 0xceb84e9fd0a49a60LU, 0x20bf8789b57f6a91LU, 0x5e2332efbdfa86ebLU },
            { 0x05017bb4eb9c21b1LU, 0x1fbfa8b6c8cd6444LU, 0x2969d7638335eb59LU, 0x6f51c81fe6160790LU },
            { 0xb111fe1560733b30LU, 0x16010e086db16febLU, 0xfcb527b00aaa9de5LU, 0x9e7078912213f6efLU },
            { 0x5f0564bea972c16eLU, 0x3c96a8ea4778734aLU, 0x28b01e6ae9968fb3LU, 0x0970867931d700aeLU },
            { 0x1974ede07597749aLU, 0xaf16f2f8d8527448LU, 0xf3be7db0fe807f1dLU, 0xc97fae4ba2516408LU },
            { 0x3c5c9fe803f69af3LU, 0x5d2fbe764a80fa7fLU, 0x5ced7949a12ab4a1LU, 0xef23ea8441cf5c53LU },
            { 0xffb5a3079c5f3418LU, 0x3373d7f543f1ab0dLU, 0x8d84012afc9aa746LU, 0xb287a6f25e5acdf8LU },
        };
        //private static uint uct_hash_size = UCT_HASH_SIZE;
        //private static uint uct_hash_limit = UCT_HASH_SIZE * 9 / 10;
        //public static void patternHash(Pattern_t pat, Pattern_hash_t hash_pat)
        //{
        //    uint[] md2_transp = new uint[16], md3_transp = new uint[16], md4_transp = new uint[16];
        //    ulong[] md5_transp = new ulong[16];
        //    uint tmp2, min2, tmp3, min3;
        //    ulong tmp4, min4, tmp5, min5;
        //    int index2, index3, index4, index5;
        //    MD2Transpose16(pat.list[(int)MD_2], md2_transp);
        //    MD3Transpose16(pat.list[(int)MD_3], md3_transp);
        //    MD4Transpose16(pat.list[(int)MD_4], md4_transp);
        //    MD5Transpose16(pat.large_list[(int)MD_5], md5_transp);
        //    index2 = index3 = index4 = index5 = 0;
        //    min2 = md2_transp[0];
        //    min3 = md3_transp[0] + md2_transp[0];
        //    min4 = (ulong)md4_transp[0] + md3_transp[0] + md2_transp[0];
        //    min5 = md5_transp[0] + md4_transp[0] + md3_transp[0] + md2_transp[0];
        //    for (int i = 1; i < 16; i++)
        //    {
        //        tmp2 = md2_transp[i];
        //        if (min2 > tmp2)
        //        {
        //            index2 = i;
        //            min2 = tmp2;
        //        }
        //        tmp3 = md3_transp[i] + md2_transp[i];
        //        if (min3 > tmp3)
        //        {
        //            index3 = i;
        //            min3 = tmp3;
        //        }
        //        tmp4 = (ulong)md4_transp[i] + md3_transp[i] + md2_transp[i];
        //        if (min4 > tmp4)
        //        {
        //            index4 = i;
        //            min4 = tmp4;
        //        }
        //        tmp5 = md5_transp[i] + md4_transp[i] + md3_transp[i] + md2_transp[i];
        //        if (min5 > tmp5)
        //        {
        //            index5 = i;
        //            min5 = tmp5;
        //        }
        //    }
        //    hash_pat.list[(int)MD_2] = MD2Hash(md2_transp[index2]);
        //    hash_pat.list[(int)MD_3] = MD3Hash(md3_transp[index3]) ^ MD2Hash(md2_transp[index3]);
        //    hash_pat.list[(int)MD_4] = MD4Hash(md4_transp[index4]) ^ MD3Hash(md3_transp[index4]) ^ MD2Hash(md2_transp[index4]);
        //    hash_pat.list[(int)MD_5 + (int)MD_MAX] = MD5Hash(md5_transp[index5]) ^ MD4Hash(md4_transp[index5]) ^ MD3Hash(md3_transp[index5]) ^ MD2Hash(md2_transp[index5]);
        //}
        public static ulong MD2Hash(uint md2)
        {
            ulong hash = 0;
            for (int i = 0; i < 12; i++)
            {
                hash ^= random_bitstrings[i, (md2 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD3Hash(uint md3)
        {
            ulong hash = 0;
            for (int i = 0; i < 12; i++)
            {
                hash ^= random_bitstrings[i + 12, (md3 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD4Hash(uint md4)
        {
            ulong hash = 0;
            for (int i = 0; i < 16; i++)
            {
                hash ^= random_bitstrings[i + 24, (md4 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD5Hash(ulong md5)
        {
            ulong hash = 0;
            for (int i = 0; i < 20; i++)
            {
                hash ^= random_bitstrings[i + 40, (md5 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static void InitializeHash()
        {
            if (!File.Exists(HashBitpath) || !File.Exists(ShapeBitpath))
            {
                Random rnd = new Random(0x7F2358FF);
                byte[] bytearray = new byte[sizeof(ulong)];
                for (int i = 0; i < BOARD_MAX; i++)
                {
                    rnd.NextBytes(bytearray);
                    hash_bit[i, (int)HASH_PASS] = BitConverter.ToUInt64(bytearray, 0);
                    rnd.NextBytes(bytearray);
                    hash_bit[i, (int)HASH_BLACK] = BitConverter.ToUInt64(bytearray, 0);
                    rnd.NextBytes(bytearray);
                    hash_bit[i, (int)HASH_WHITE] = BitConverter.ToUInt64(bytearray, 0);
                    rnd.NextBytes(bytearray);
                    hash_bit[i, (int)HASH_KO] = BitConverter.ToUInt64(bytearray, 0);
                    rnd.NextBytes(bytearray);
                    shape_bit[i] = BitConverter.ToUInt64(bytearray, 0);
                }
                Save();
            }
            else
            {
                Load();
            }

        }
        public static void Save()
        {
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(HashBitpath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, hash_bit);
                stream.Close();
            }
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(ShapeBitpath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, shape_bit);
                stream.Close();
            }
        }
        public static void Load()
        {
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(HashBitpath, FileMode.Open, FileAccess.Read, FileShare.Read);
                hash_bit = formatter.Deserialize(stream) as ulong[,];
                stream.Close();
            }
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(ShapeBitpath, FileMode.Open, FileAccess.Read, FileShare.Read);
                shape_bit = formatter.Deserialize(stream) as ulong[];
                stream.Close();
            }

        }
        public static int TRANS20(ulong hash) { return (int)(((hash & 0xFFFFFFFF) ^ ((hash >> 32) & 0xFFFFFFFF)) & 0xFFFFF); }
    };
}
