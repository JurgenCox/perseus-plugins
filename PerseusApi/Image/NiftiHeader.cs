using Accord;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using BaseLibS.Util;
namespace PerseusApi.Image{
	[Serializable]
	public class NiftiHeader : ICloneable{
		// see https://brainder.org/2012/09/23/the-nifti-file-format/
		public int header_size{ get; set; }
		public bool notUsed1{ get; set; }
		public char dim_info{ get; set; }
		public short[] dim{ get; set; }
		public float intent_p1{ get; set; }
		public float intent_p2{ get; set; }
		public float intent_p3{ get; set; }
		public short intent_code{ get; set; }
		public short datatype{ get; set; }
		public short bitpix{ get; set; }
		public short slice_start{ get; set; }
		public float[] pixdim{ get; set; }
		public float voxel_offset{ get; set; }
		public float scl_slope{ get; set; }
		public float scl_inter{ get; set; }
		public short slice_end{ get; set; }
		public char slice_code{ get; set; }
		public char xyzt_units{ get; set; }
		public float cal_max{ get; set; }
		public float cal_min{ get; set; }
		public float slice_duration{ get; set; }
		public float toffset{ get; set; }
		public bool notUsed2{ get; set; }
		public string descrip{ get; set; }
		public string aux_file{ get; set; }
		public short qform_code{ get; set; }
		public short sform_code{ get; set; }
		public float quatern_b{ get; set; }
		public float quatern_c{ get; set; }
		public float quatern_d{ get; set; }
		public float qoffset_x{ get; set; }
		public float qoffset_y{ get; set; }
		public float qoffset_z{ get; set; }
		public float[] srow_x{ get; set; }
		public float[] srow_y{ get; set; }
		public float[] srow_z{ get; set; }
		public string intent_name{ get; set; }
		public string magic{ get; set; }
		public NiftiHeader(BinaryReader reader) { 
			header_size = reader.ReadInt32(); 
			notUsed1 = reader.ReadBoolean(); 
			dim_info = reader.ReadChar();
			dim = FileUtils.ReadInt16Array(reader);
			intent_p1 = reader.ReadSingle();
			intent_p2 = reader.ReadSingle();
			intent_p3 = reader.ReadSingle();
			intent_code = reader.ReadInt16();
			datatype = reader.ReadInt16();
			bitpix = reader.ReadInt16();
			slice_start = reader.ReadInt16();
			pixdim = FileUtils.ReadFloatArray(reader);
			voxel_offset = reader.ReadSingle();
			scl_slope = reader.ReadSingle();
			scl_inter = reader.ReadSingle();
			slice_end = reader.ReadInt16();
			slice_code = reader.ReadChar();
			xyzt_units = reader.ReadChar();
			cal_max = reader.ReadSingle();
			cal_min = reader.ReadSingle();
			slice_duration = reader.ReadSingle();
			toffset = reader.ReadSingle();
			notUsed2 = reader.ReadBoolean();
			descrip = reader.ReadString();
			aux_file = reader.ReadString();
			qform_code = reader.ReadInt16();
			sform_code = reader.ReadInt16();
			quatern_b = reader.ReadSingle();
			quatern_c = reader.ReadSingle();
			quatern_d = reader.ReadSingle();
			qoffset_x = reader.ReadSingle();
			qoffset_y = reader.ReadSingle();
			qoffset_z = reader.ReadSingle();
			srow_x = FileUtils.ReadFloatArray(reader);
			srow_y = FileUtils.ReadFloatArray(reader);
			srow_z = FileUtils.ReadFloatArray(reader);
			intent_name = reader.ReadString();
			magic = reader.ReadString();
		}
		public NiftiHeader(){
		}
		public void Write(BinaryWriter writer){
			writer.Write(header_size);
			writer.Write(notUsed1);
			writer.Write(dim_info);
			FileUtils.Write(dim, writer);
			writer.Write(intent_p1);
			writer.Write(intent_p2);
			writer.Write(intent_p3);
			writer.Write(intent_code);
			writer.Write(datatype);
			writer.Write(bitpix);
			writer.Write(slice_start);
			FileUtils.Write(pixdim, writer);
			writer.Write(voxel_offset);
			writer.Write(scl_slope);
			writer.Write(scl_inter);
			writer.Write(slice_end);
			writer.Write(slice_code);
			writer.Write(xyzt_units);
			writer.Write(cal_max);
			writer.Write(cal_min);
			writer.Write(slice_duration);
			writer.Write(toffset);
			writer.Write(notUsed2);
			writer.Write(descrip);
			writer.Write(aux_file);
			writer.Write(qform_code);
			writer.Write(sform_code);
			writer.Write(quatern_b);
			writer.Write(quatern_c);
			writer.Write(quatern_d);
			writer.Write(qoffset_x);
			writer.Write(qoffset_y);
			writer.Write(qoffset_z);
			FileUtils.Write(srow_x, writer);
			FileUtils.Write(srow_y, writer);
			FileUtils.Write(srow_z, writer);
			writer.Write(intent_name);
			writer.Write(magic);
		}
		public (bool, string) ReadNiftiHeader(string path, MriMetadata mrimetadata){
			// check if there is a file as indicated by path. 
			if (File.Exists(path)){
				// if zipped
				if (path.Substring(path.Length - 7) == ".nii.gz"){
					using (Stream fileStream = File.OpenRead(path),
					       zippedStream = new GZipStream(fileStream, CompressionMode.Decompress)){
						// nifti files are binary
						using (BinaryReader reader = new BinaryReader(zippedStream)){
							(bool valid, string errorMessage) = FillHead(reader);
							if (!valid){
								return (false, errorMessage);
							}
						}
					}
				}
				// if not zipped
				else{
					// nifti files are binary
					using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))){
						(bool valid, string errorMessage) = FillHead(reader);
						if (!valid){
							return (false, errorMessage);
						}
					}
				}
			} else{
				return (false, "File doesn't exist. Wrong path: " + path);
			}

			// for ReadHead()
			(bool, string) FillHead(BinaryReader reader){
				// read as as bytearray
				byte[] input = reader.ReadBytes(348);

				// for strings
				Encoding ascii = Encoding.ASCII;

				// check if correct magic number. 
				string newmagic = ascii.GetString(input, 344, 4);
				if (newmagic.Contains("ni1") || newmagic.Contains("n+1")){
					// Check if correct header size
					int headersize = BitConverter.ToInt32(input, 0);
					if (headersize == 348){
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
						for (int i = 0; i < 8; i++){
							dimensions[i] = BitConverter.ToInt16(input, (i) * 2 + 40);
							pixdims[i] = BitConverter.ToSingle(input, (i) * 4 + 76);
							if (i > 3){
								continue;
							}
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
						if (units > 31){
							return (false, "\"units\" is larger than 31.");
						}
						// 24 = mys
						if (units > 23){
							mrimetadata.t_unit = (float) 0.000001;
							units = units - 24;
						}
						// 16 = ms
						else if (units > 15){
							mrimetadata.t_unit = (float) 0.001;
							units = units - 16;
						}
						// 8 = s
						else if (units > 7){
							mrimetadata.t_unit = 1;
							units = units - 8;
						}
						// 3 = mym
						if (units == 3){
							mrimetadata.xyz_unit = (float) 0.001;
						}
						// 2 = mm
						else if (units == 2){
							mrimetadata.xyz_unit = 1;
						}
						// 1 = m
						else if (units == 1){
							mrimetadata.xyz_unit = 1000;
						}
					} else{
						return (false, "This doesn't look like a correct nifti file. Header size not 348.");
					}
				} else{
					return (false, "This doesn't look like a correct nifti file. Magic number is: " + newmagic);
				}
				return (true, String.Empty);
			}
			return (true, String.Empty);
		}

		// TODO NEW TESTING
		public float[,,,][,,] GetData2(string path){
			byte[,,,][] input = new byte[dim[7], dim[6], dim[5], dim[4]][];
			int[,,,] lengths = new int[input.GetLength(0), input.GetLength(1), input.GetLength(2), input.GetLength(3)];
			for (int i = 0; i < lengths.GetLength(0); i++){
				for (int j = 0; j < lengths.GetLength(1); j++){
					for (int k = 0; k < lengths.GetLength(2); k++){
						for (int l = 0; l < lengths.GetLength(3); l++){
							lengths[i, j, k, l] = dim[1] * dim[2] * dim[3] * bitpix / 8;
						}
					}
				}
			}

			// if zipped
			if (path.Substring(path.Length - 7) == ".nii.gz"){
				using (Stream fileStream = File.OpenRead(path),
				       zippedStream = new GZipStream(fileStream, CompressionMode.Decompress)){
					// reading in
					using (BinaryReader reader = new BinaryReader(zippedStream)){
						reader.ReadBytes((int) voxel_offset); // drop offset;
						for (int i = 0; i < lengths.GetLength(0); i++){
							for (int j = 0; j < lengths.GetLength(1); j++){
								for (int k = 0; k < lengths.GetLength(2); k++){
									for (int l = 0; l < lengths.GetLength(3); l++){
										input[i, j, k, l] = reader.ReadBytes(lengths[i, j, k, l]);
									}
								}
							}
						}
					}
				}
			}
			// if not zipped
			else{
				// reading in
				using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))){
					reader.ReadBytes((int) voxel_offset); // drop offset;
					for (int i = 0; i < lengths.GetLength(0); i++){
						for (int j = 0; j < lengths.GetLength(1); j++){
							for (int k = 0; k < lengths.GetLength(2); k++){
								for (int l = 0; l < lengths.GetLength(3); l++){
									input[i, j, k, l] = reader.ReadBytes(lengths[i, j, k, l]);
								}
							}
						}
					}
				}
			}

			// make and fill array

			// make array
			float[,,,][,,] result = new float[dim[7], dim[6], dim[5], dim[4]][,,];
			for (int i = 0; i < lengths.GetLength(0); i++){
				for (int j = 0; j < lengths.GetLength(1); j++){
					for (int k = 0; k < lengths.GetLength(2); k++){
						for (int l = 0; l < lengths.GetLength(3); l++){
							result[i, j, k, l] = new float[dim[1], dim[2], dim[3]];
						}
					}
				}
			}

			// fill array
			for (int a = 0; a < dim[7]; a++){
				for (int b = 0; b < dim[6]; b++){
					for (int c = 0; c < dim[5]; c++){
						for (int i = 0; i < dim[4]; i++){
							for (int j = 0; j < dim[3]; j++){
								for (int k = 0; k < dim[2]; k++){
									for (int l = 0; l < dim[1]; l++){
										int cur_loc = (j * (dim[1] * dim[2]) + k * dim[1] + l) * bitpix / 8;

										// Check which datatype and read in
										// 4 = signed short
										if (datatype == 4){
											result[a, b, c, i][l, k, j] =
												(float) BitConverter.ToInt16(input[a, b, c, i], cur_loc);
										}
										// 8 = signed int
										else if (datatype == 8){
											result[a, b, c, i][l, k, j] =
												(float) BitConverter.ToInt32(input[a, b, c, i], cur_loc);
										}
										// 16 = float
										else if (datatype == 16){
											result[a, b, c, i][l, k, j] =
												(float) BitConverter.ToSingle(input[a, b, c, i], cur_loc);
										}
										// 512 = unsigned short
										else if (datatype == 512){
											result[a, b, c, i][l, k, j] =
												(float) BitConverter.ToUInt16(input[a, b, c, i], cur_loc);
										}
										// 768 = unsigned int
										else if (datatype == 768){
											result[a, b, c, i][l, k, j] =
												(float) BitConverter.ToUInt32(input[a, b, c, i], cur_loc);
										} else{
											throw new Exception("Format not supported.");
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		public float[][,,] GetData(string path){
			byte[][] input = new byte[dim[4]][];
			int[] lengths = new int[dim[4]];
			for (int i = 0; i < lengths.Length; i++){
				lengths[i] = dim[1] * dim[2] * dim[3] * bitpix / 8;
			}

			// if zipped
			if (path.Substring(path.Length - 7) == ".nii.gz"){
				using (Stream fileStream = File.OpenRead(path),
				       zippedStream = new GZipStream(fileStream, CompressionMode.Decompress)){
					// reading in
					using (BinaryReader reader = new BinaryReader(zippedStream)){
						reader.ReadBytes((int) voxel_offset); // drop offset;
						for (int i = 0; i < input.Length; i++){
							input[i] = reader.ReadBytes(lengths[i]);
						}
					}
				}
			}
			// if not zipped
			else{
				// reading in
				using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))){
					reader.ReadBytes((int) voxel_offset); // drop offset;
					for (int i = 0; i < input.Length; i++){
						input[i] = reader.ReadBytes(lengths[i]);
					}
				}
			}

			// make and fill array
			float[][,,] result = new float[dim[4]][,,];
			for (int i = 0; i < result.Length; i++){
				result[i] = new float[dim[1], dim[2], dim[3]];
			}
			int a = 0;
			for (int i = 0; i < dim[4]; i++){
				for (int j = 0; j < dim[3]; j++){
					for (int k = 0; k < dim[2]; k++){
						for (int l = 0; l < dim[1]; l++){
							int cur_loc = (j * (dim[1] * dim[2]) + k * dim[1] + l) * bitpix / 8;

							// Check which datatype and read in
							// 2 = bytes
							if (datatype == 2){
								result[i][l, k, j] = Convert.ToSingle(input[i][0 + a]);
								a = a + 1;
							}
							// 4 = signed short
							else if (datatype == 4){
								result[i][l, k, j] = (float) BitConverter.ToInt16(input[i], cur_loc);
							}
							// 8 = signed int
							else if (datatype == 8){
								result[i][l, k, j] = (float) BitConverter.ToInt32(input[i], cur_loc);
							}
							// 16 = float
							else if (datatype == 16){
								result[i][l, k, j] = (float) BitConverter.ToSingle(input[i], cur_loc);
							}
							// 512 = unsigned short
							else if (datatype == 512){
								result[i][l, k, j] = (float) BitConverter.ToUInt16(input[i], cur_loc);
							}
							// 768 = unsigned int
							else if (datatype == 768){
								result[i][l, k, j] = (float) BitConverter.ToUInt32(input[i], cur_loc);
							} else{
								Console.WriteLine("Data format not supported.");
								return result;
							}
						}
					}
				}
			}
			return result;
		}
		public void WriteNiftiFile(string path, float[][,,] data){
			// check if path has .nii extension. Add if not. 
			if (path.Length < 5 || path.Substring(path.Length - 4, 4) != ".nii"){
				path += ".nii";
			}

			// write nifti file
			using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))){
				// write header (see https://brainder.org/2012/09/23/the-nifti-file-format/)
				foreach (PropertyInfo prop in typeof(NiftiHeader).GetProperties()){
					Type type = prop.GetValue(this, null).GetType();
					Object value = prop.GetValue(this, null);
					if (type == typeof(Int32)){
						writer.Write((Int32) value);
					} else if (type == typeof(Int16)){
						writer.Write((Int16) value);
					} else if (type == typeof(Boolean)){
						if (prop.Name == "notUsed1"){
							byte[] bytearray = new byte[35];
							for (int i = 0; i < 35; i++){
								bytearray[i] = 0;
							}
							writer.Write(bytearray);
						} else if (prop.Name == "notUsed2"){
							byte[] bytearray = new byte[8];
							for (int i = 0; i < 8; i++){
								bytearray[i] = 0;
							}
							writer.Write(bytearray);
						}
					} else if (type == typeof(Char)){
						writer.Write((byte) (char) value);
					} else if (type == typeof(float)){
						writer.Write((float) value);
					} else if (type == typeof(string)){
						byte[] byterepresentation = Encoding.ASCII.GetBytes((string) value);
						writer.Write(byterepresentation);
					} else if (type == typeof(Int16[])){
						Int16[] list = (Int16[]) value;
						for (int i = 0; i < list.Length; i++){
							writer.Write(list[i]);
						}
					} else if (type == typeof(Single[])){
						Single[] list = (Single[]) value;
						for (int i = 0; i < list.Length; i++){
							writer.Write(list[i]);
						}
					}
				}

				// write data
				writer.Seek((int) voxel_offset, SeekOrigin.Begin);
				for (int i = 0; i < dim[4]; i++){
					for (int j = 0; j < dim[3]; j++){
						for (int k = 0; k < dim[2]; k++){
							for (int l = 0; l < dim[1]; l++){
								// Check which datatype and read in
								// 4 = signed short
								if (datatype == 4){
									writer.Write((Int16) data[i][l, k, j]);
								}
								// 8 = signed int
								else if (datatype == 8){
									writer.Write((Int32) data[i][l, k, j]);
								}
								// 16 = float
								else if (datatype == 16){
									writer.Write((Single) data[i][l, k, j]);
								}
								// 512 = unsigned short
								else if (datatype == 512){
									writer.Write((UInt16) data[i][l, k, j]);
								}
								// 768 = unsigned int
								else if (datatype == 768){
									writer.Write((UInt32) data[i][l, k, j]);
								}
							}
						}
					}
				}
			}
		}

		//TODO unique command that reads all nifti
		//public void WriteNiftiFile2(string path, float[,,,][,,] data) {
		//    // check if path has .nii extension. Add if not. 
		//    if (path.Length < 5 || path.Substring(path.Length - 4, 4) != ".nii") {
		//        path += ".nii";
		//    }

		//    // write nifti file
		//    using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
		//        // write header (see https://brainder.org/2012/09/23/the-nifti-file-format/)
		//        foreach (PropertyInfo prop in typeof(NiftiHeader).GetProperties()) {
		//            Type type = prop.GetValue(this, null).GetType();
		//            Object value = prop.GetValue(this, null);

		//            if (type == typeof(Int32)) {
		//                writer.Write((Int32)value);
		//            }
		//            else if (type == typeof(Int16)) {
		//                writer.Write((Int16)value);
		//            }
		//            else if (type == typeof(Boolean)) {
		//                if (prop.Name == "notUsed1") {
		//                    byte[] bytearray = new byte[35];
		//                    for (int i = 0; i < 35; i++) {
		//                        bytearray[i] = 0;
		//                    }
		//                    writer.Write(bytearray);
		//                }
		//                else if (prop.Name == "notUsed2") {
		//                    byte[] bytearray = new byte[8];
		//                    for (int i = 0; i < 8; i++) {
		//                        bytearray[i] = 0;
		//                    }
		//                    writer.Write(bytearray);
		//                }
		//            }
		//            else if (type == typeof(Char)) {
		//                writer.Write((byte)(char)value);
		//            }
		//            else if (type == typeof(float)) {
		//                writer.Write((float)value);
		//            }
		//            else if (type == typeof(string)) {
		//                byte[] byterepresentation = Encoding.ASCII.GetBytes((string)value);
		//                writer.Write(byterepresentation);
		//            }
		//            else if (type == typeof(Int16[])) {
		//                Int16[] list = (Int16[])value;
		//                for (int i = 0; i < list.Length; i++) {
		//                    writer.Write(list[i]);
		//                }
		//            }
		//            else if (type == typeof(Single[])) {
		//                Single[] list = (Single[])value;
		//                for (int i = 0; i < list.Length; i++) {
		//                    writer.Write(list[i]);
		//                }
		//            }
		//        }

		//        // write data
		//        writer.Seek((int)voxel_offset, SeekOrigin.Begin);

		//        for (int a = 0; a < dim[7]; a++) {
		//            for (int b = 0; b < dim[5]; b++) {
		//                for (int c = 0; c < dim[6]; c++) {
		//                    for (int i = 0; i < dim[4]; i++) {
		//                        for (int j = 0; j < dim[3]; j++) {
		//                            for (int k = 0; k < dim[2]; k++) {
		//                                for (int l = 0; l < dim[1]; l++) {
		//                                    // Check which datatype and read in
		//                                    // 4 = signed short
		//                                    if (datatype == 4) {
		//                                        writer.Write((Int16)data[a,b,c,i][l, k, j]);
		//                                    }
		//                                    // 8 = signed int
		//                                    else if (datatype == 8) {
		//                                        writer.Write((Int32)data[a,b,c,i][l, k, j]);
		//                                    }
		//                                    // 16 = float
		//                                    else if (datatype == 16) {
		//                                        writer.Write((Single)data[a,b,c,i][l, k, j]);
		//                                    }
		//                                    // 512 = unsigned short
		//                                    else if (datatype == 512) {
		//                                        writer.Write((UInt16)data[a,b,c,i][l, k, j]);
		//                                    }
		//                                    // 768 = unsigned int
		//                                    else if (datatype == 768) {
		//                                        writer.Write((UInt32)data[a,b,c,i][l, k, j]);
		//                                    }
		//                                }
		//                            }
		//                        }
		//                    }
		//                }
		//            }
		//        }
		//    }
		//}
		public void WriteNiftiFile5D(string path, float[,][,,] data){
			// check if path has .nii extension. Add if not. 
			if (path.Length < 5 || path.Substring(path.Length - 4, 4) != ".nii"){
				path += ".nii";
			}

			// write nifti file
			using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))){
				// write header (see https://brainder.org/2012/09/23/the-nifti-file-format/)
				foreach (PropertyInfo prop in typeof(NiftiHeader).GetProperties()){
					Type type = prop.GetValue(this, null).GetType();
					Object value = prop.GetValue(this, null);
					if (type == typeof(Int32)){
						writer.Write((Int32) value);
					} else if (type == typeof(Int16)){
						writer.Write((Int16) value);
					} else if (type == typeof(Boolean)){
						if (prop.Name == "notUsed1"){
							byte[] bytearray = new byte[35];
							for (int i = 0; i < 35; i++){
								bytearray[i] = 0;
							}
							writer.Write(bytearray);
						} else if (prop.Name == "notUsed2"){
							byte[] bytearray = new byte[8];
							for (int i = 0; i < 8; i++){
								bytearray[i] = 0;
							}
							writer.Write(bytearray);
						}
					} else if (type == typeof(Char)){
						writer.Write((byte) (char) value);
					} else if (type == typeof(float)){
						writer.Write((float) value);
					} else if (type == typeof(string)){
						byte[] byterepresentation = Encoding.ASCII.GetBytes((string) value);
						writer.Write(byterepresentation);
					} else if (type == typeof(Int16[])){
						Int16[] list = (Int16[]) value;
						for (int i = 0; i < list.Length; i++){
							writer.Write(list[i]);
						}
					} else if (type == typeof(Single[])){
						Single[] list = (Single[]) value;
						for (int i = 0; i < list.Length; i++){
							writer.Write(list[i]);
						}
					}
				}

				// write data
				writer.Seek((int) voxel_offset, SeekOrigin.Begin);
				for (int a = 0; a < dim[5]; a++){
					for (int b = 0; b < dim[4]; b++){
						for (int c = 0; c < dim[3]; c++){
							for (int i = 0; i < dim[2]; i++){
								for (int j = 0; j < dim[1]; j++){
									// Check which datatype and read in
									// 4 = signed short
									if (datatype == 4){
										writer.Write((Int16) data[b, a][j, i, c]);
									}
									// 8 = signed int
									else if (datatype == 8){
										writer.Write((Int32) data[b, a][j, i, c]);
									}
									// 16 = float
									else if (datatype == 16){
										writer.Write((Single) data[b, a][j, i, c]);
									}
									// 512 = unsigned short
									else if (datatype == 512){
										writer.Write((UInt16) data[b, a][j, i, c]);
									}
									// 768 = unsigned int
									else if (datatype == 768){
										writer.Write((UInt32) data[b, a][j, i, c]);
									}
								}
							}
						}
					}
				}
			}
		}
		public float[,,] ReduceData(float[][,,] olddata){
			return olddata[0];
		}
		public float[,,] ReduceData(float[,,] olddata){
			return olddata;
		}
		public object Clone(){
			NiftiHeader clone = new NiftiHeader(){
				header_size = header_size,
				notUsed1 = notUsed1,
				dim_info = dim_info,
				dim = (short[]) dim?.Clone(),
				intent_p1 = intent_p1,
				intent_p2 = intent_p2,
				intent_p3 = intent_p3,
				intent_code = intent_code,
				datatype = datatype,
				bitpix = bitpix,
				slice_start = slice_start,
				pixdim = (float[]) pixdim?.Clone(),
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
				srow_x = (float[]) srow_x?.Clone(),
				srow_y = (float[]) srow_y?.Clone(),
				srow_z = (float[]) srow_z?.Clone(),
				intent_name = intent_name,
				magic = magic,
			};
			return clone;
		}
	}
}