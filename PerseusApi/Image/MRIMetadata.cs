using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.IO.Compression;



namespace PerseusApi.Image
{
    public class MRIMetadata : ICloneable
    {
        // see https://brainder.org/2012/09/23/the-nifti-file-format/
        public int header_size { get; set; }
        public Boolean notUsed1 { get; set; }
        public char dim_info { get; set; }
        public short[] dim { get; set; }
        public float intent_p1 { get; set; }
        public float intent_p2 { get; set; }
        public float intent_p3 { get; set; }
        public short intent_code { get; set; }
        public short datatype { get; set; }
        public short bitpix { get; set; }
        public short slice_start { get; set; }
        public float[] pixdim { get; set; }
        public float voxel_offset { get; set; }
        public float scl_slope { get; set; }
        public float scl_inter { get; set; }
        public short slice_end { get; set; }
        public char slice_code { get; set; }
        public char xyzt_units { get; set; }
        public float cal_max { get; set; }
        public float cal_min { get; set; }
        public float slice_duration { get; set; }
        public float toffset { get; set; }
        public Boolean notUsed2 { get; set; }
        public string descrip { get; set; }
        public string aux_file { get; set; }
        public short qform_code { get; set; }
        public short sform_code { get; set; }
        public float quatern_b { get; set; }
        public float quatern_c { get; set; }
        public float quatern_d { get; set; }
        public float qoffset_x { get; set; }
        public float qoffset_y { get; set; }
        public float qoffset_z { get; set; }
        public float[] srow_x { get; set; }
        public float[] srow_y { get; set; }
        public float[] srow_z { get; set; }
        public string intent_name { get; set; }
        public string magic { get; set; }

        // special
        public float xyz_unit { get; set; }
        public float t_unit { get; set; }


        public  void ReadNiftiHeader(string path)
        {
            // check if there is a file as indicated by path. 
            if (File.Exists(path))
            {
                // if zipped
                if (path.Substring(path.Length - 7) == ".nii.gz")
                {
                    using (Stream fileStream = File.OpenRead(path),
                        zippedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        // nifti files are binary
                        using (BinaryReader reader = new BinaryReader(zippedStream))
                        {
                            FillHead(reader);
                        }
                    }
                }
                // if not zipped
                else
                {
                    // nifti files are binary
                    using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                    {
                        FillHead(reader);
                    }
                }
            }
            else
            {
                Console.WriteLine("File doesn't exist.");
                Console.WriteLine("Wrong path: {0}", path);
            }

            // for ReadHead()
            void FillHead(BinaryReader reader)
            {
                // read as as bytearray
                byte[] input = reader.ReadBytes(348);

                // for strings
                Encoding ascii = Encoding.ASCII;

                // check if correct magic number. 
                string newmagic = ascii.GetString(input, 344, 4);
                if (newmagic.Contains("ni1") || newmagic.Contains("n+1"))
                {
                    // Check if correct header size
                    int headersize = BitConverter.ToInt32(input, 0);
                    if (headersize == 348)
                    {
                        // set values
                        magic = newmagic;
                        header_size = headersize;
                        dim_info = BitConverter.ToChar(input, 39);
                        intent_p1 = BitConverter.ToSingle(input, 56);
                        intent_p2 = BitConverter.ToSingle(input, 60);
                        intent_p3 = BitConverter.ToSingle(input, 64);
                        intent_code = BitConverter.ToInt16(input, 68);
                        datatype = BitConverter.ToInt16(input, 70);
                        bitpix = BitConverter.ToInt16(input, 72);
                        slice_start = BitConverter.ToInt16(input, 74);
                        voxel_offset = BitConverter.ToSingle(input, 108);
                        scl_slope = BitConverter.ToSingle(input, 112);
                        scl_inter = BitConverter.ToSingle(input, 116);
                        slice_end = BitConverter.ToInt16(input, 120);
                        slice_code = BitConverter.ToChar(input, 122);
                        xyzt_units = BitConverter.ToChar(input, 123);
                        cal_max = BitConverter.ToSingle(input, 124);
                        cal_min = BitConverter.ToSingle(input, 128);
                        slice_duration = BitConverter.ToSingle(input, 132);
                        toffset = BitConverter.ToSingle(input, 136);
                        descrip = ascii.GetString(input, 148, 80);
                        aux_file = ascii.GetString(input, 228, 24);
                        qform_code = BitConverter.ToInt16(input, 252);
                        sform_code = BitConverter.ToInt16(input, 254);
                        quatern_b = BitConverter.ToSingle(input, 256);
                        quatern_c = BitConverter.ToSingle(input, 260);
                        quatern_d = BitConverter.ToSingle(input, 264);
                        qoffset_x = BitConverter.ToSingle(input, 268);
                        qoffset_y = BitConverter.ToSingle(input, 272);
                        qoffset_z = BitConverter.ToSingle(input, 276);
                        intent_name = ascii.GetString(input, 328, 16);
                        float[] srowx = new float[4];
                        float[] srowy = new float[4];
                        float[] srowz = new float[4];
                        short[] dimensions = new short[8];
                        float[] pixdims = new float[8];
                        for (int i = 0; i < 8; i++)
                        {
                            dimensions[i] = BitConverter.ToInt16(input, (i) * 2 + 40);
                            pixdims[i] = BitConverter.ToSingle(input, (i) * 4 + 76);
                            if (i > 3) { continue; }
                            srowx[i] = BitConverter.ToSingle(input, (i) * 4 + 280);
                            srowy[i] = BitConverter.ToSingle(input, (i) * 4 + 296);
                            srowz[i] = BitConverter.ToSingle(input, (i) * 4 + 312);
                        }
                        pixdim = pixdims;
                        dim = dimensions;
                        srow_x = srowx;
                        srow_y = srowy;
                        srow_z = srowz;

                        // set specials: xyz_unit & t_unit
                        int units = input[123];

                        // over 31 is not supported
                        if (units > 31)
                        {
                            return;
                        }
                        // 24 = mys
                        if (units > 23)
                        {
                            t_unit = (float)0.000001;
                            units = units - 24;
                        }
                        // 16 = ms
                        else if (units > 15)
                        {
                            t_unit = (float)0.001;
                            units = units - 16;
                        }
                        // 8 = s
                        else if (units > 7)
                        {
                            t_unit = 1;
                            units = units - 8;
                        }
                        // 3 = mym
                        if (units == 3)
                        {
                            xyz_unit = (float)0.001;
                        }
                        // 2 = mm
                        else if (units == 2)
                        {
                            xyz_unit = 1;
                        }
                        // 1 = m
                        else if (units == 1)
                        {
                            xyz_unit = 1000;
                        }
                    }
                    else
                    {
                        Console.WriteLine("This doesn't look like a correct nifti file. Header size not 348.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("This doesn't look like a nifti file.");
                    Console.WriteLine("Magic number is: {0}", newmagic);
                    return;
                }
            }
        }

        public float[,,,] GetDataFromNifti(string path)
        {
            byte[] input;
            int length = (int)voxel_offset + dim[1] * dim[2] * dim[3] * dim[4] * bitpix;

            // if zipped
            if (path.Substring(path.Length - 7) == ".nii.gz")
            {
                using (Stream fileStream = File.OpenRead(path),
                        zippedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    // reading in
                    using (BinaryReader reader = new BinaryReader(zippedStream))
                    {
                        input = reader.ReadBytes(length);
                    }
                }
            }
            // if not zipped
            else
            {
                // reading in
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    input = reader.ReadBytes(length);
                }
            }

            // make and fill array
            float[,,,] result = new float[dim[4], dim[1], dim[2], dim[3]];
            for (int i = 0; i < dim[4]; i++)
            {
                for (int j = 0; j < dim[3]; j++)
                {
                    for (int k = 0; k < dim[2]; k++)
                    {
                        for (int l = 0; l < dim[1]; l++)
                        {
                            int cur_loc = (int)voxel_offset + (i * (dim[1] * dim[2] * dim[3]) + j * (dim[1] * dim[2]) + k * dim[1] + l) * bitpix / 8;

                            // Check which datatype and read in
                            // 4 = signed short
                            if (datatype == 4)
                            {
                                result[i, l, k, j] = (float)BitConverter.ToInt16(input, cur_loc);
                            }
                            // 8 = signed int
                            else if (datatype == 8)
                            {
                                result[i, l, k, j] = (float)BitConverter.ToInt32(input, cur_loc);
                            }
                            // 16 = float
                            else if (datatype == 16)
                            {
                                result[i, l, k, j] = (float)BitConverter.ToSingle(input, cur_loc);
                            }
                            // 512 = unsigned short
                            else if (datatype == 512)
                            {
                                result[i, l, k, j] = (float)BitConverter.ToUInt16(input, cur_loc);
                            }
                            // 768 = unsigned int
                            else if (datatype == 768)
                            {
                                result[i, l, k, j] = (float)BitConverter.ToUInt32(input, cur_loc);
                            }
                            else
                            {
                                Console.WriteLine("Data format not supported.");
                                return result;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public void WriteNiftiFile(string path, float[,,,] data)
        {
            // check if path has .nii extension. Add if not. 
            if (path.Length < 5 || path.Substring(path.Length - 4, 4) != ".nii")
            {
                path += ".nii";
            }

            // write nifti file
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                // write header (see https://brainder.org/2012/09/23/the-nifti-file-format/)
                foreach (PropertyInfo prop in typeof(MRIMetadata).GetProperties())
                {
                    Type type = prop.GetValue(this, null).GetType();
                    Object value = prop.GetValue(this, null);

                    if (type == typeof(Int32))
                    {
                        writer.Write((Int32)value);
                    }
                    else if (type == typeof(Int16))
                    {
                        writer.Write((Int16)value);
                    }
                    else if (type == typeof(Boolean))
                    {
                        if (prop.Name == "notUsed1")
                        {
                            byte[] bytearray = new byte[35];
                            for (int i = 0; i < 35; i++)
                            {
                                bytearray[i] = 0;
                            }
                            writer.Write(bytearray);
                        }
                        else if (prop.Name == "notUsed2")
                        {
                            byte[] bytearray = new byte[8];
                            for (int i = 0; i < 8; i++)
                            {
                                bytearray[i] = 0;
                            }
                            writer.Write(bytearray);
                        }
                    }
                    else if (type == typeof(Char))
                    {
                        writer.Write((byte)(char)value);
                    }
                    else if (type == typeof(float))
                    {
                        if (prop.Name != "xyz_unit" && prop.Name != "t_unit")
                        {
                            writer.Write((float)value);
                        }
                    }
                    else if (type == typeof(string))
                    {
                        byte[] byterepresentation = Encoding.ASCII.GetBytes((string)value);
                        writer.Write(byterepresentation);
                    }
                    else if (type == typeof(Int16[]))
                    {
                        Int16[] list = (Int16[])value;
                        for (int i = 0; i < list.Length; i++)
                        {
                            writer.Write(list[i]);
                        }
                    }
                    else if (type == typeof(Single[]))
                    {
                        Single[] list = (Single[])value;
                        for (int i = 0; i < list.Length; i++)
                        {
                            writer.Write(list[i]);
                        }
                    }
                }

                // write data
                writer.Seek((int)voxel_offset, SeekOrigin.Begin);

                for (int i = 0; i < dim[4]; i++)
                {
                    for (int j = 0; j < dim[3]; j++)
                    {
                        for (int k = 0; k < dim[2]; k++)
                        {
                            for (int l = 0; l < dim[1]; l++)
                            {
                                // Check which datatype and read in
                                // 4 = signed short
                                if (datatype == 4)
                                {
                                    writer.Write((Int16)data[i, l, k, j]);
                                }
                                // 8 = signed int
                                else if (datatype == 8)
                                {
                                    writer.Write((Int32)data[i, l, k, j]);
                                }
                                // 16 = float
                                else if (datatype == 16)
                                {
                                    writer.Write((Single)data[i, l, k, j]);
                                }
                                // 512 = unsigned short
                                else if (datatype == 512)
                                {
                                    writer.Write((UInt16)data[i, l, k, j]);
                                }
                                // 768 = unsigned int
                                else if (datatype == 768)
                                {
                                    writer.Write((UInt32)data[i, l, k, j]);
                                }
                            }
                        }
                    }
                }
            }
        }


        public float[,,] ReduceData(float[,,,] olddata)
        {
            float[,,] reduced_data = new float[dim[1], dim[2], dim[3]];
            for (int x = 0; x < dim[1]; x++)
            {
                for (int y = 0; y < dim[2]; y++)
                {
                    for (int z = 0; z < dim[3]; z++)
                    {
                        reduced_data[x, y, z] = olddata[0, x, y, z];
                    }
                }
            }
            return reduced_data;
        }

        public float[,,] ReduceData(float[,,] olddata)
        {
            return olddata;
        }

        public object Clone()
        {
            MRIMetadata clone = new MRIMetadata()
            {
                header_size = header_size,
                notUsed1 = notUsed1,
                dim_info = dim_info,
                dim = (short[])dim?.Clone(),
                intent_p1 = intent_p1,
                intent_p2 = intent_p2,
                intent_p3 = intent_p3,
                intent_code = intent_code,
                datatype = datatype,
                bitpix = bitpix,
                slice_start = slice_start,
                pixdim = (float[])pixdim?.Clone(),
                voxel_offset = voxel_offset,
                scl_slope = scl_slope,
                scl_inter = scl_inter,
                slice_end = slice_end,
                slice_code = slice_code,
                xyzt_units = xyzt_units,
                cal_max = cal_max,
                cal_min = cal_min,
                slice_duration = slice_duration,
                toffset = toffset,
                notUsed2 = notUsed2,
                descrip = descrip,
                aux_file = aux_file,
                qform_code = qform_code,
                sform_code = sform_code,
                quatern_b = quatern_b,
                quatern_c = quatern_c,
                quatern_d = quatern_d,
                qoffset_x = qoffset_x,
                qoffset_y = qoffset_y,
                qoffset_z = qoffset_z,
                srow_x = (float[])srow_x?.Clone(),
                srow_y = (float[])srow_y?.Clone(),
                srow_z = (float[])srow_z?.Clone(),
                intent_name = intent_name,
                magic = magic,

                // special
                xyz_unit = xyz_unit,
                t_unit = t_unit
            };
            return clone;
        } 

    }
}
