using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using SQE.Backend.DataAccess.Helpers;

namespace SQE.Backend.DataAccess.Models.Native
{
    public class SQENative
    {
        #region Abstract class definitions

        public abstract class TableTemplate
        {
            public string TableName { get; set; }

            public abstract ListDictionary ColumsAndValues();


            private string Snake(string str)
            {
                return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
            }
        }

        public abstract class NonEditableTableTemplate : TableTemplate
        {
            //What should I do, if anything to make this immutable?
        }

        public abstract class EditableTableTemplate : TableTemplate
        {
            public string PrimaryKey { get; set; }
        }

        public abstract class UserEditableTableTemplate : EditableTableTemplate
        {
            public Helpers.Action action { get; set; }
        }

        public abstract class OwnedTableTemplate : UserEditableTableTemplate
        {
            public string OwnerTable { get; set; }
        }

        public abstract class OwnerTableTemplate : EditableTableTemplate
        {
            public string OwnedTable { get; set; }
        }

        public abstract class AuthoredTableTemplate : UserEditableTableTemplate
        {
            public string AuthorTable { get; set; }
        }

        public abstract class AuthorTableTemplate : EditableTableTemplate
        {
            public string AuthoredTable { get; set; }
        }
        #endregion

        #region Table class definitions
        public class SqeImage : AuthoredTableTemplate
        {
            public uint? SqeImageId { get; set; }
            public uint? ImageUrlsId { get; set; }
            public string Filename { get; set; }
            public uint? NativeWidth { get; set; }
            public uint? NativeHeight { get; set; }
            public uint? Dpi { get; set; }
            public bool? Type { get; set; }
            public ushort? WavelengthStart { get; set; }
            public ushort? WavelengthEnd { get; set; }
            public bool? IsMaster { get; set; }
            public uint? ImageCatalogId { get; set; }
            public bool? IsRecto { get; set; }

            public SqeImage(uint? sqe_image_id, uint? image_urls_id, string filename, uint? native_width, uint? native_height, uint? dpi, bool? type, ushort? wavelength_start, ushort? wavelength_end, bool? is_master, uint? image_catalog_id, bool? is_recto)
            {
                TableName = "SQE_image";
                AuthorTable = "SQE_image_author";
                PrimaryKey = "SQE_image_id";
                SqeImageId = sqe_image_id;
                ImageUrlsId = image_urls_id;
                Filename = filename;
                NativeWidth = native_width;
                NativeHeight = native_height;
                Dpi = dpi;
                Type = type;
                WavelengthStart = wavelength_start;
                WavelengthEnd = wavelength_end;
                IsMaster = is_master;
                ImageCatalogId = image_catalog_id;
                IsRecto = is_recto;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sqe_image_id", SqeImageId },
            {"image_urls_id", ImageUrlsId},
            {"filename", Filename},
            {"native_width", NativeWidth},
            {"native_height", NativeHeight},
            {"dpi", Dpi},
            {"type", Type},
            {"wavelength_start", WavelengthStart},
            {"wavelength_end", WavelengthEnd},
            {"is_master", IsMaster},
            {"image_catalog_id", ImageCatalogId},
            {"is_recto", IsRecto},
        };
                return dict;
            }
        }

        public class SqeImageAuthor : AuthorTableTemplate
        {
            public uint? SqeImageId { get; set; }
            public ushort? UserId { get; set; }

            public SqeImageAuthor(uint? sqe_image_id, ushort? user_id)
            {
                TableName = "SQE_image_author";
                AuthoredTable = "SQE_image";
                SqeImageId = sqe_image_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sqe_image_id", SqeImageId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class AreaGroup : OwnedTableTemplate
        {
            public uint? AreaGroupId { get; set; }
            public uint? AreaId { get; set; }
            public string Name { get; set; }
            public string Commentary { get; set; }
            public bool? ZIndex { get; set; }

            public AreaGroup(uint? area_group_id, uint? area_id, string name, string commentary, bool? z_index)
            {
                TableName = "area_group";
                OwnerTable = "area_group_owner";
                PrimaryKey = "area_group_id";
                AreaGroupId = area_group_id;
                AreaId = area_id;
                Name = name;
                Commentary = commentary;
                ZIndex = z_index;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"area_group_id", AreaGroupId},
            {"area_id", AreaId},
            {"name", Name},
            {"commentary", Commentary},
            {"z_index", ZIndex},
        };
                return dict;
            }
        }

        public class AreaGroupMember : NonEditableTableTemplate
        {
            public uint? AreaGroupId { get; set; }
            public int? AreaId { get; set; }
            public string AreaType { get; set; }

            public AreaGroupMember(uint? area_group_id, int? area_id, string area_type)
            {
                TableName = "area_group_member";
                AreaGroupId = area_group_id;
                AreaId = area_id;
                AreaType = area_type;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"area_group_id", AreaGroupId},
            {"area_id", AreaId},
            {"area_type", AreaType},
        };
                return dict;
            }
        }

        public class AreaGroupOwner : OwnerTableTemplate
        {
            public uint? AreaGroupId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public AreaGroupOwner(uint? area_group_id, uint? scroll_version_id)
            {
                TableName = "area_group_owner";
                OwnedTable = "area_group";
                AreaGroupId = area_group_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"area_group_id", AreaGroupId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class Artefact : NonEditableTableTemplate
        {
            public uint? ArtefactId { get; set; }

            public Artefact(uint? artefact_id)
            {
                TableName = "artefact";
                ArtefactId = artefact_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_id", ArtefactId},
        };
                return dict;
            }
        }

        public class ArtefactData : OwnedTableTemplate
        {
            public uint? ArtefactDataId { get; set; }
            public uint? ArtefactId { get; set; }
            public string Name { get; set; }

            public ArtefactData(uint? artefact_data_id, uint? artefact_id, string name)
            {
                TableName = "artefact_data";
                OwnerTable = "artefact_data_owner";
                PrimaryKey = "artefact_data_id";
                ArtefactDataId = artefact_data_id;
                ArtefactId = artefact_id;
                Name = name;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_data_id", ArtefactDataId},
            {"artefact_id", ArtefactId},
            {"name", Name},
        };
                return dict;
            }
        }

        public class ArtefactDataOwner : OwnerTableTemplate
        {
            public uint? ArtefactDataId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ArtefactDataOwner(uint? artefact_data_id, uint? scroll_version_id)
            {
                TableName = "artefact_data_owner";
                OwnedTable = "artefact_data";
                ArtefactDataId = artefact_data_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_data_id", ArtefactDataId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ArtefactPosition : OwnedTableTemplate
        {
            public uint? ArtefactPositionId { get; set; }
            public uint? ArtefactId { get; set; }
            public string TransformMatrix { get; set; }
            public bool? ZIndex { get; set; }

            public ArtefactPosition(uint? artefact_position_id, uint? artefact_id, string transform_matrix, bool? z_index)
            {
                TableName = "artefact_position";
                OwnerTable = "artefact_position_owner";
                PrimaryKey = "artefact_position_id";
                ArtefactPositionId = artefact_position_id;
                ArtefactId = artefact_id;
                TransformMatrix = transform_matrix;
                ZIndex = z_index;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_position_id", ArtefactPositionId},
            {"artefact_id", ArtefactId},
            {"transform_matrix", TransformMatrix},
            {"z_index", ZIndex},
        };
                return dict;
            }
        }

        public class ArtefactPositionOwner : OwnerTableTemplate
        {
            public uint? ArtefactPositionId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ArtefactPositionOwner(uint? artefact_position_id, uint? scroll_version_id)
            {
                TableName = "artefact_position_owner";
                OwnedTable = "artefact_position";
                ArtefactPositionId = artefact_position_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_position_id", ArtefactPositionId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ArtefactShape : OwnedTableTemplate
        {
            public uint? ArtefactShapeId { get; set; }
            public uint? ArtefactId { get; set; }
            public uint? IdOfSqeImage { get; set; }
            public List<byte> RegionInSqeImage { get; set; }

            public ArtefactShape(uint? artefact_shape_id, uint? artefact_id, uint? id_of_sqe_image, List<byte> region_in_sqe_image)
            {
                TableName = "artefact_shape";
                OwnerTable = "artefact_shape_owner";
                PrimaryKey = "artefact_shape_id";
                ArtefactShapeId = artefact_shape_id;
                ArtefactId = artefact_id;
                IdOfSqeImage = id_of_sqe_image;
                RegionInSqeImage = region_in_sqe_image;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_shape_id", ArtefactShapeId},
            {"artefact_id", ArtefactId},
            {"id_of_sqe_image", IdOfSqeImage},
            {"region_in_sqe_image", RegionInSqeImage},
        };
                return dict;
            }
        }

        public class ArtefactShapeOwner : OwnerTableTemplate
        {
            public uint? ArtefactShapeId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ArtefactShapeOwner(uint? artefact_shape_id, uint? scroll_version_id)
            {
                TableName = "artefact_shape_owner";
                OwnedTable = "artefact_shape";
                ArtefactShapeId = artefact_shape_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_shape_id", ArtefactShapeId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ArtefactStack : OwnedTableTemplate
        {
            public uint? ArtefactStackId { get; set; }
            public uint? ArtefactAId { get; set; }
            public uint? ArtefactBId { get; set; }
            public bool? LayerA { get; set; }
            public bool? LayerB { get; set; }
            public bool? AIsVerso { get; set; }
            public bool? BIsVerso { get; set; }
            public bool? Shared { get; set; }

            public ArtefactStack(uint? artefact_stack_id, uint? artefact_A_id, uint? artefact_B_id, bool? layer_A, bool? layer_B, bool? A_is_verso, bool? B_is_verso, bool? shared)
            {
                TableName = "artefact_stack";
                OwnerTable = "artefact_stack_owner";
                PrimaryKey = "artefact_stack_id";
                ArtefactStackId = artefact_stack_id;
                ArtefactAId = artefact_A_id;
                ArtefactBId = artefact_B_id;
                LayerA = layer_A;
                LayerB = layer_B;
                AIsVerso = A_is_verso;
                BIsVerso = B_is_verso;
                Shared = shared;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_stack_id", ArtefactStackId},
            {"artefact_A_id", ArtefactAId},
            {"artefact_B_id", ArtefactBId},
            {"layer_A", LayerA},
            {"layer_B", LayerB},
            {"A_is_verso", AIsVerso},
            {"B_is_verso", BIsVerso},
            {"shared", Shared},
        };
                return dict;
            }
        }

        public class ArtefactStackOwner : OwnerTableTemplate
        {
            public uint? ArtefactStackId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ArtefactStackOwner(uint? artefact_stack_id, uint? scroll_version_id)
            {
                TableName = "artefact_stack_owner";
                OwnedTable = "artefact_stack";
                ArtefactStackId = artefact_stack_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"artefact_stack_id", ArtefactStackId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class Attribute : NonEditableTableTemplate
        {
            public uint? AttributeId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            public Attribute(uint? attribute_id, string name, string description)
            {
                TableName = "attribute";
                AttributeId = attribute_id;
                Name = name;
                Description = description;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"attribute_id", AttributeId},
            {"name", Name},
            {"description", Description},
        };
                return dict;
            }
        }

        public class AttributeNumeric : NonEditableTableTemplate
        {
            public uint? SignCharAttributeId { get; set; }
            public float? Value { get; set; }

            public AttributeNumeric(uint? sign_char_attribute_id, float? value)
            {
                TableName = "attribute_numeric";
                SignCharAttributeId = sign_char_attribute_id;
                Value = value;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_attribute_id", SignCharAttributeId},
            {"value", Value},
        };
                return dict;
            }
        }

        public class AttributeValue : NonEditableTableTemplate
        {
            public uint? AttributeValueId { get; set; }
            public uint? AttributeId { get; set; }
            public string StringValue { get; set; }
            public string Description { get; set; }

            public AttributeValue(uint? attribute_value_id, uint? attribute_id, string string_value, string description)
            {
                TableName = "attribute_value";
                AttributeValueId = attribute_value_id;
                AttributeId = attribute_id;
                StringValue = string_value;
                Description = description;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"attribute_value_id", AttributeValueId},
            {"attribute_id", AttributeId},
            {"string_value", StringValue},
            {"description", Description},
        };
                return dict;
            }
        }

        public class AttributeValueCss : NonEditableTableTemplate
        {
            public uint? AttributeValueCssId { get; set; }
            public uint? AttributeValueId { get; set; }
            public string Css { get; set; }

            public AttributeValueCss(uint? attribute_value_css_id, uint? attribute_value_id, string css)
            {
                TableName = "attribute_value_css";
                AttributeValueCssId = attribute_value_css_id;
                AttributeValueId = attribute_value_id;
                Css = css;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"attribute_value_css_id", AttributeValueCssId},
            {"attribute_value_id", AttributeValueId},
            {"css", Css},
        };
                return dict;
            }
        }

        public class CharOfWriting : OwnedTableTemplate
        {
            public uint? CharOfWritingId { get; set; }
            public uint? FormOfWritingId { get; set; }
            public string UnicodeChar { get; set; }
            public short? LineOffset { get; set; }

            public CharOfWriting(uint? char_of_writing_id, uint? form_of_writing_id, string unicode_char, short? line_offset)
            {
                TableName = "char_of_writing";
                OwnerTable = "char_of_writing_owner";
                PrimaryKey = "char_of_writing_id";
                CharOfWritingId = char_of_writing_id;
                FormOfWritingId = form_of_writing_id;
                UnicodeChar = unicode_char;
                LineOffset = line_offset;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"char_of_writing_id", CharOfWritingId},
            {"form_of_writing_id", FormOfWritingId},
            {"unicode_char", UnicodeChar},
            {"line_offset", LineOffset},
        };
                return dict;
            }
        }

        public class CharOfWritingOwner : OwnerTableTemplate
        {
            public uint? CharOfWritingId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public CharOfWritingOwner(uint? char_of_writing_id, uint? scroll_version_id)
            {
                TableName = "char_of_writing_owner";
                OwnedTable = "char_of_writing";
                CharOfWritingId = char_of_writing_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"char_of_writing_id", CharOfWritingId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class Col : NonEditableTableTemplate
        {
            public uint? ColId { get; set; }

            public Col(uint? col_id)
            {
                TableName = "col";
                ColId = col_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_id", ColId},
        };
                return dict;
            }
        }

        public class ColData : OwnedTableTemplate
        {
            public uint? ColDataId { get; set; }
            public uint? ColId { get; set; }
            public string Name { get; set; }

            public ColData(uint? col_data_id, uint? col_id, string name)
            {
                TableName = "col_data";
                OwnerTable = "col_data_owner";
                PrimaryKey = "col_data_id";
                ColDataId = col_data_id;
                ColId = col_id;
                Name = name;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_data_id", ColDataId},
            {"col_id", ColId},
            {"name", Name},
        };
                return dict;
            }
        }

        public class ColDataOwner : OwnerTableTemplate
        {
            public uint? ColDataId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ColDataOwner(uint? col_data_id, uint? scroll_version_id)
            {
                TableName = "col_data_owner";
                OwnedTable = "col_data";
                ColDataId = col_data_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_data_id", ColDataId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ColSequence : OwnedTableTemplate
        {
            public uint? ColSequenceId { get; set; }
            public uint? ColId { get; set; }
            public ushort? Position { get; set; }

            public ColSequence(uint? col_sequence_id, uint? col_id, ushort? position)
            {
                TableName = "col_sequence";
                OwnerTable = "col_sequence_owner";
                PrimaryKey = "col_sequence_id";
                ColSequenceId = col_sequence_id;
                ColId = col_id;
                Position = position;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_sequence_id", ColSequenceId},
            {"col_id", ColId},
            {"position", Position},
        };
                return dict;
            }
        }

        public class ColSequenceOwner : OwnerTableTemplate
        {
            public uint? ColSequenceId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ColSequenceOwner(uint? col_sequence_id, uint? scroll_version_id)
            {
                TableName = "col_sequence_owner";
                OwnedTable = "col_sequence";
                ColSequenceId = col_sequence_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_sequence_id", ColSequenceId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ColToLine : OwnedTableTemplate
        {
            public uint? ColToLineId { get; set; }
            public uint? ColId { get; set; }
            public uint? LineId { get; set; }

            public ColToLine(uint? col_to_line_id, uint? col_id, uint? line_id)
            {
                TableName = "col_to_line";
                OwnerTable = "col_to_line_owner";
                PrimaryKey = "col_to_line_id";
                ColToLineId = col_to_line_id;
                ColId = col_id;
                LineId = line_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_to_line_id", ColToLineId},
            {"col_id", ColId},
            {"line_id", LineId},
        };
                return dict;
            }
        }

        public class ColToLineOwner : OwnerTableTemplate
        {
            public uint? ColToLineId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ColToLineOwner(uint? col_to_line_id, uint? scroll_version_id)
            {
                TableName = "col_to_line_owner";
                OwnedTable = "col_to_line";
                ColToLineId = col_to_line_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"col_to_line_id", ColToLineId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class EditionCatalog : AuthoredTableTemplate
        {
            public uint? EditionCatalogId { get; set; }
            public string Manuscript { get; set; }
            public string EditionName { get; set; }
            public string EditionVolume { get; set; }
            public string EditionLocation1 { get; set; }
            public string EditionLocation2 { get; set; }
            public bool? EditionSide { get; set; }
            public uint? ScrollId { get; set; }
            public string Comment { get; set; }

            public EditionCatalog(uint? edition_catalog_id, string manuscript, string edition_name, string edition_volume, string edition_location_1, string edition_location_2, bool? edition_side, uint? scroll_id, string comment)
            {
                TableName = "edition_catalog";
                AuthorTable = "edition_catalog_author";
                PrimaryKey = "edition_catalog_id";
                EditionCatalogId = edition_catalog_id;
                Manuscript = manuscript;
                EditionName = edition_name;
                EditionVolume = edition_volume;
                EditionLocation1 = edition_location_1;
                EditionLocation2 = edition_location_2;
                EditionSide = edition_side;
                ScrollId = scroll_id;
                Comment = comment;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"edition_catalog_id", EditionCatalogId},
            {"manuscript", Manuscript},
            {"edition_name", EditionName},
            {"edition_volume", EditionVolume},
            {"edition_location_1", EditionLocation1},
            {"edition_location_2", EditionLocation2},
            {"edition_side", EditionSide},
            {"scroll_id", ScrollId},
            {"comment", Comment},
        };
                return dict;
            }
        }

        public class EditionCatalogAuthor : AuthorTableTemplate
        {
            public uint? EditionCatalogId { get; set; }
            public ushort? UserId { get; set; }

            public EditionCatalogAuthor(uint? edition_catalog_id, ushort? user_id)
            {
                TableName = "edition_catalog_author";
                AuthoredTable = "edition_catalog";
                EditionCatalogId = edition_catalog_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"edition_catalog_id", EditionCatalogId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class EditionCatalogToCol : NonEditableTableTemplate
        {
            public uint? EditionCatalogToColId { get; set; }
            public uint? EditionCatalogId { get; set; }
            public uint? ColId { get; set; }
            public uint? UserId { get; set; }

            public EditionCatalogToCol(uint? edition_catalog_to_col_id, uint? edition_catalog_id, uint? col_id, uint? user_id)
            {
                TableName = "edition_catalog_to_col";
                EditionCatalogToColId = edition_catalog_to_col_id;
                EditionCatalogId = edition_catalog_id;
                ColId = col_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"edition_catalog_to_col_id", EditionCatalogToColId},
            {"edition_catalog_id", EditionCatalogId},
            {"col_id", ColId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class EditionCatalogToColConfirmation : NonEditableTableTemplate
        {
            public uint? EditionCatalogToColId { get; set; }
            public bool? Confirmed { get; set; }
            public ushort? UserId { get; set; }
            public DateTime? Time { get; set; }

            public EditionCatalogToColConfirmation(uint? edition_catalog_to_col_id, bool? confirmed, ushort? user_id, DateTime? time)
            {
                TableName = "edition_catalog_to_col_confirmation";
                EditionCatalogToColId = edition_catalog_to_col_id;
                Confirmed = confirmed;
                UserId = user_id;
                Time = time;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"edition_catalog_to_col_id", EditionCatalogToColId},
            {"confirmed", Confirmed},
            {"user_id", UserId},
            {"time", Time},
        };
                return dict;
            }
        }

        public class ExternalFont : NonEditableTableTemplate
        {
            public uint? ExternalFontId { get; set; }
            public string FontId { get; set; }

            public ExternalFont(uint? external_font_id, string font_id)
            {
                TableName = "external_font";
                ExternalFontId = external_font_id;
                FontId = font_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"external_font_id", ExternalFontId},
            {"font_id", FontId},
        };
                return dict;
            }
        }

        public class ExternalFontGlyph : NonEditableTableTemplate
        {
            public uint? ExternalFontGlyphId { get; set; }
            public uint? ExternalFontId { get; set; }
            public List<byte> UnicodeChar { get; set; }
            public List<byte> Path { get; set; }
            public ushort? Width { get; set; }
            public ushort? Height { get; set; }

            public ExternalFontGlyph(uint? external_font_glyph_id, uint? external_font_id, List<byte> unicode_char, List<byte> path, ushort? width, ushort? height)
            {
                TableName = "external_font_glyph";
                ExternalFontGlyphId = external_font_glyph_id;
                ExternalFontId = external_font_id;
                UnicodeChar = unicode_char;
                Path = path;
                Width = width;
                Height = height;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"external_font_glyph_id", ExternalFontGlyphId},
            {"external_font_id", ExternalFontId},
            {"unicode_char", UnicodeChar},
            {"path", Path},
            {"width", Width},
            {"height", Height},
        };
                return dict;
            }
        }

        public class FormOfWriting : OwnedTableTemplate
        {
            public uint? FormOfWritingId { get; set; }
            public uint? ScribesScribeId { get; set; }
            public bool? Pen { get; set; }
            public bool? Ink { get; set; }
            public uint? ScribalFontTypeId { get; set; }

            public FormOfWriting(uint? form_of_writing_id, uint? scribes_scribe_id, bool? pen, bool? ink, uint? scribal_font_type_id)
            {
                TableName = "form_of_writing";
                OwnerTable = "form_of_writing_owner";
                PrimaryKey = "form_of_writing_id";
                FormOfWritingId = form_of_writing_id;
                ScribesScribeId = scribes_scribe_id;
                Pen = pen;
                Ink = ink;
                ScribalFontTypeId = scribal_font_type_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"form_of_writing_id", FormOfWritingId},
            {"scribes_scribe_id", ScribesScribeId},
            {"pen", Pen},
            {"ink", Ink},
            {"scribal_font_type_id", ScribalFontTypeId},
        };
                return dict;
            }
        }

        public class FormOfWritingOwner : OwnerTableTemplate
        {
            public uint? FormOfWritingId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public FormOfWritingOwner(uint? form_of_writing_id, uint? scroll_version_id)
            {
                TableName = "form_of_writing_owner";
                OwnedTable = "form_of_writing";
                FormOfWritingId = form_of_writing_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"form_of_writing_id", FormOfWritingId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ImageCatalog : AuthoredTableTemplate
        {
            public uint? ImageCatalogId { get; set; }
            public string Institution { get; set; }
            public string CatalogNumber1 { get; set; }
            public string CatalogNumber2 { get; set; }
            public bool? CatalogSide { get; set; }

            public ImageCatalog(uint? image_catalog_id, string institution, string catalog_number_1, string catalog_number_2, bool? catalog_side)
            {
                TableName = "image_catalog";
                AuthorTable = "image_catalog_author";
                PrimaryKey = "image_catalog_id";
                ImageCatalogId = image_catalog_id;
                Institution = institution;
                CatalogNumber1 = catalog_number_1;
                CatalogNumber2 = catalog_number_2;
                CatalogSide = catalog_side;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"image_catalog_id", ImageCatalogId},
            {"institution", Institution},
            {"catalog_number_1", CatalogNumber1},
            {"catalog_number_2", CatalogNumber2},
            {"catalog_side", CatalogSide},
        };
                return dict;
            }
        }

        public class ImageCatalogAuthor : AuthorTableTemplate
        {
            public uint? ImageCatalogId { get; set; }
            public ushort? UserId { get; set; }

            public ImageCatalogAuthor(uint? image_catalog_id, ushort? user_id)
            {
                TableName = "image_catalog_author";
                AuthoredTable = "image_catalog";
                ImageCatalogId = image_catalog_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"image_catalog_id", ImageCatalogId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class ImageToEditionCatalog : NonEditableTableTemplate
        {
            public uint? EditionCatalogId { get; set; }
            public uint? ImageCatalogId { get; set; }

            public ImageToEditionCatalog(uint? edition_catalog_id, uint? image_catalog_id)
            {
                TableName = "image_to_edition_catalog";
                EditionCatalogId = edition_catalog_id;
                ImageCatalogId = image_catalog_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"edition_catalog_id", EditionCatalogId},
            {"image_catalog_id", ImageCatalogId},
        };
                return dict;
            }
        }

        public class ImageToImageMap : AuthoredTableTemplate
        {
            public uint? ImageToImageMapId { get; set; }
            public uint? Image1Id { get; set; }
            public uint? Image2Id { get; set; }
            public List<byte> RegionOnImage1 { get; set; }
            public List<byte> RegionOnImage2 { get; set; }
            public double? Rotation { get; set; }

            public ImageToImageMap(uint? image_to_image_map_id, uint? image1_id, uint? image2_id, List<byte> region_on_image1, List<byte> region_on_image2, double? rotation)
            {
                TableName = "image_to_image_map";
                AuthorTable = "image_to_image_map_author";
                PrimaryKey = "image_to_image_map_id";
                ImageToImageMapId = image_to_image_map_id;
                Image1Id = image1_id;
                Image2Id = image2_id;
                RegionOnImage1 = region_on_image1;
                RegionOnImage2 = region_on_image2;
                Rotation = rotation;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"image_to_image_map_id", ImageToImageMapId},
            {"image1_id", Image1Id},
            {"image2_id", Image2Id},
            {"region_on_image1", RegionOnImage1},
            {"region_on_image2", RegionOnImage2},
            {"rotation", Rotation},
        };
                return dict;
            }
        }

        public class ImageToImageMapAuthor : AuthorTableTemplate
        {
            public uint? ImageToImageMapId { get; set; }
            public ushort? UserId { get; set; }

            public ImageToImageMapAuthor(uint? image_to_image_map_id, ushort? user_id)
            {
                TableName = "image_to_image_map_author";
                AuthoredTable = "image_to_image_map";
                ImageToImageMapId = image_to_image_map_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"image_to_image_map_id", ImageToImageMapId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class ImageUrls : NonEditableTableTemplate
        {
            public uint? ImageUrlsId { get; set; }
            public string Url { get; set; }
            public string Suffix { get; set; }
            public string Proxy { get; set; }
            public string License { get; set; }

            public ImageUrls(uint? image_urls_id, string url, string suffix, string proxy, string license)
            {
                TableName = "image_urls";
                ImageUrlsId = image_urls_id;
                Url = url;
                Suffix = suffix;
                Proxy = proxy;
                License = license;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"image_urls_id", ImageUrlsId},
            {"url", Url},
            {"suffix", Suffix},
            {"proxy", Proxy},
            {"license", License},
        };
                return dict;
            }
        }

        public class KerningOfChar : NonEditableTableTemplate
        {
            public short? Kerning { get; set; }
            public string PreviousChar { get; set; }
            public uint? CharsOfWritingCharOfWritingId { get; set; }

            public KerningOfChar(short? kerning, string previous_char, uint? chars_of_writing_char_of_writing_id)
            {
                TableName = "kerning_of_char";
                Kerning = kerning;
                PreviousChar = previous_char;
                CharsOfWritingCharOfWritingId = chars_of_writing_char_of_writing_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"kerning", Kerning},
            {"previous_char", PreviousChar},
            {"chars_of_writing_char_of_writing_id", CharsOfWritingCharOfWritingId},
        };
                return dict;
            }
        }

        public class Line : NonEditableTableTemplate
        {
            public uint? LineId { get; set; }

            public Line(uint? line_id)
            {
                TableName = "line";
                LineId = line_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"line_id", LineId},
        };
                return dict;
            }
        }

        public class LineData : OwnedTableTemplate
        {
            public uint? LineDataId { get; set; }
            public uint? LineId { get; set; }
            public string Name { get; set; }

            public LineData(uint? line_data_id, uint? line_id, string name)
            {
                TableName = "line_data";
                OwnerTable = "line_data_owner";
                PrimaryKey = "line_data_id";
                LineDataId = line_data_id;
                LineId = line_id;
                Name = name;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"line_data_id", LineDataId},
            {"line_id", LineId},
            {"name", Name},
        };
                return dict;
            }
        }

        public class LineDataOwner : OwnerTableTemplate
        {
            public uint? LineDataId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public LineDataOwner(uint? line_data_id, uint? scroll_version_id)
            {
                TableName = "line_data_owner";
                OwnedTable = "line_data";
                LineDataId = line_data_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"line_data_id", LineDataId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class LineToSign : OwnedTableTemplate
        {
            public uint? LineToSignId { get; set; }
            public uint? SignId { get; set; }
            public uint? LineId { get; set; }

            public LineToSign(uint? line_to_sign_id, uint? sign_id, uint? line_id)
            {
                TableName = "line_to_sign";
                OwnerTable = "line_to_sign_owner";
                PrimaryKey = "line_to_sign_id";
                LineToSignId = line_to_sign_id;
                SignId = sign_id;
                LineId = line_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"line_to_sign_id", LineToSignId},
            {"sign_id", SignId},
            {"line_id", LineId},
        };
                return dict;
            }
        }

        public class LineToSignOwner : OwnerTableTemplate
        {
            public uint? LineToSignId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public LineToSignOwner(uint? line_to_sign_id, uint? scroll_version_id)
            {
                TableName = "line_to_sign_owner";
                OwnedTable = "line_to_sign";
                LineToSignId = line_to_sign_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"line_to_sign_id", LineToSignId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class MainAction : NonEditableTableTemplate
        {
            public uint? MainActionId { get; set; }
            public DateTime? Time { get; set; }
            public bool? Rewinded { get; set; }
            public uint? ScrollVersionId { get; set; }

            public MainAction(uint? main_action_id, DateTime? time, bool? rewinded, uint? scroll_version_id)
            {
                TableName = "main_action";
                MainActionId = main_action_id;
                Time = time;
                Rewinded = rewinded;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"main_action_id", MainActionId},
            {"time", Time},
            {"rewinded", Rewinded},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ParallelGroup : NonEditableTableTemplate
        {
            public uint? ParallelGroupId { get; set; }

            public ParallelGroup(uint? parallel_group_id)
            {
                TableName = "parallel_group";
                ParallelGroupId = parallel_group_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"parallel_group_id", ParallelGroupId},
        };
                return dict;
            }
        }

        public class ParallelWord : OwnedTableTemplate
        {
            public uint? ParallelWordId { get; set; }
            public uint? WordId { get; set; }
            public uint? ParallelGroupId { get; set; }
            public bool? SubGroup { get; set; }

            public ParallelWord(uint? parallel_word_id, uint? word_id, uint? parallel_group_id, bool? sub_group)
            {
                TableName = "parallel_word";
                OwnerTable = "parallel_word_owner";
                PrimaryKey = "parallel_word_id";
                ParallelWordId = parallel_word_id;
                WordId = word_id;
                ParallelGroupId = parallel_group_id;
                SubGroup = sub_group;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"parallel_word_id", ParallelWordId},
            {"word_id", WordId},
            {"parallel_group_id", ParallelGroupId},
            {"sub_group", SubGroup},
        };
                return dict;
            }
        }

        public class ParallelWordOwner : OwnerTableTemplate
        {
            public uint? ParallelWordId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ParallelWordOwner(uint? parallel_word_id, uint? scroll_version_id)
            {
                TableName = "parallel_word_owner";
                OwnedTable = "parallel_word";
                ParallelWordId = parallel_word_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"parallel_word_id", ParallelWordId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class PointToPointMap : NonEditableTableTemplate
        {
            public uint? PointToPointMapId { get; set; }
            public uint? ImageToImageMapId { get; set; }

            public PointToPointMap(uint? point_to_point_map_id, uint? image_to_image_map_id)
            {
                TableName = "point_to_point_map";
                PointToPointMapId = point_to_point_map_id;
                ImageToImageMapId = image_to_image_map_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"point_to_point_map_id", PointToPointMapId},
            {"image_to_image_map_id", ImageToImageMapId},
        };
                return dict;
            }
        }

        public class PositionInStream : OwnedTableTemplate
        {
            public uint? PositionInStreamId { get; set; }
            public uint? SignId { get; set; }
            public uint? NextSignId { get; set; }

            public PositionInStream(uint? position_in_stream_id, uint? sign_id, uint? next_sign_id)
            {
                TableName = "position_in_stream";
                OwnerTable = "position_in_stream_owner";
                PrimaryKey = "position_in_stream_id";
                PositionInStreamId = position_in_stream_id;
                SignId = sign_id;
                NextSignId = next_sign_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"position_in_stream_id", PositionInStreamId},
            {"sign_id", SignId},
            {"next_sign_id", NextSignId},
        };
                return dict;
            }
        }

        public class PositionInStreamOwner : OwnerTableTemplate
        {
            public uint? PositionInStreamId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public PositionInStreamOwner(uint? position_in_stream_id, uint? scroll_version_id)
            {
                TableName = "position_in_stream_owner";
                OwnedTable = "position_in_stream";
                PositionInStreamId = position_in_stream_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"position_in_stream_id", PositionInStreamId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class PositionInStreamToWordRel : NonEditableTableTemplate
        {
            public uint? PositionInStreamId { get; set; }
            public uint? WordId { get; set; }
            public bool? PositionInWord { get; set; }

            public PositionInStreamToWordRel(uint? position_in_stream_id, uint? word_id, bool? position_in_word)
            {
                TableName = "position_in_stream_to_word_rel";
                PositionInStreamId = position_in_stream_id;
                WordId = word_id;
                PositionInWord = position_in_word;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"position_in_stream_id", PositionInStreamId},
            {"word_id", WordId},
            {"position_in_word", PositionInWord},
        };
                return dict;
            }
        }

        public class QwbBiblio : NonEditableTableTemplate
        {
            public uint? QwbBiblioId { get; set; }
            public string BiblioShort { get; set; }

            public QwbBiblio(uint? qwb_biblio_id, string biblio_short)
            {
                TableName = "qwb_biblio";
                QwbBiblioId = qwb_biblio_id;
                BiblioShort = biblio_short;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_biblio_id", QwbBiblioId},
            {"biblio_short", BiblioShort},
        };
                return dict;
            }
        }

        public class QwbRef : NonEditableTableTemplate
        {
            public uint? QwbRefId { get; set; }
            public string RefText { get; set; }
            public ushort? BookPosition { get; set; }

            public QwbRef(uint? qwb_ref_id, string ref_text, ushort? book_position)
            {
                TableName = "qwb_ref";
                QwbRefId = qwb_ref_id;
                RefText = ref_text;
                BookPosition = book_position;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_ref_id", QwbRefId},
            {"ref_text", RefText},
            {"book_position", BookPosition},
        };
                return dict;
            }
        }

        public class QwbVariant : OwnedTableTemplate
        {
            public uint? QwbVariantId { get; set; }
            public uint? QwdWordDataId { get; set; }
            public string Text { get; set; }
            public string Lemma { get; set; }
            public string Grammar { get; set; }
            public string Meaning { get; set; }
            public uint? QwbBiblioId { get; set; }

            public QwbVariant(uint? qwb_variant_id, uint? qwd_word_data_id, string text, string lemma, string grammar, string meaning, uint? qwb_biblio_id)
            {
                TableName = "qwb_variant";
                OwnerTable = "qwb_variant_owner";
                PrimaryKey = "qwb_variant_id";
                QwbVariantId = qwb_variant_id;
                QwdWordDataId = qwd_word_data_id;
                Text = text;
                Lemma = lemma;
                Grammar = grammar;
                Meaning = meaning;
                QwbBiblioId = qwb_biblio_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_variant_id", QwbVariantId},
            {"qwd_word_data_id", QwdWordDataId},
            {"text", Text},
            {"lemma", Lemma},
            {"grammar", Grammar},
            {"meaning", Meaning},
            {"qwb_biblio_id", QwbBiblioId},
        };
                return dict;
            }
        }

        public class QwbVariantOwner : OwnerTableTemplate
        {
            public uint? QwbVariantId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public QwbVariantOwner(uint? qwb_variant_id, uint? scroll_version_id)
            {
                TableName = "qwb_variant_owner";
                OwnedTable = "qwb_variant";
                QwbVariantId = qwb_variant_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_variant_id", QwbVariantId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class QwbWordData : OwnedTableTemplate
        {
            public uint? QwbWordDataId { get; set; }
            public uint? QwbWordId { get; set; }
            public string Text { get; set; }
            public uint? Position { get; set; }
            public uint? QwbRefId { get; set; }

            public QwbWordData(uint? qwb_word_data_id, uint? qwb_word_id, string text, uint? position, uint? qwb_ref_id)
            {
                TableName = "qwb_word_data";
                OwnerTable = "qwb_word_data_owner";
                PrimaryKey = "qwb_word_data_id";
                QwbWordDataId = qwb_word_data_id;
                QwbWordId = qwb_word_id;
                Text = text;
                Position = position;
                QwbRefId = qwb_ref_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_word_data_id", QwbWordDataId},
            {"qwb_word_id", QwbWordId},
            {"text", Text},
            {"position", Position},
            {"qwb_ref_id", QwbRefId},
        };
                return dict;
            }
        }

        public class QwbWordDataOwner : OwnerTableTemplate
        {
            public uint? QwbWordDataId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public QwbWordDataOwner(uint? qwb_word_data_id, uint? scroll_version_id)
            {
                TableName = "qwb_word_data_owner";
                OwnedTable = "qwb_word_data";
                QwbWordDataId = qwb_word_data_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"qwb_word_data_id", QwbWordDataId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class RoiPosition : NonEditableTableTemplate
        {
            public uint? RoiPositionId { get; set; }
            public string TransformMatrix { get; set; }

            public RoiPosition(uint? roi_position_id, string transform_matrix)
            {
                TableName = "roi_position";
                RoiPositionId = roi_position_id;
                TransformMatrix = transform_matrix;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"roi_position_id", RoiPositionId},
            {"transform_matrix", TransformMatrix},
        };
                return dict;
            }
        }

        public class RoiShape : NonEditableTableTemplate
        {
            public uint? RoiShapeId { get; set; }
            public List<byte> Path { get; set; }

            public RoiShape(uint? roi_shape_id, List<byte> path)
            {
                TableName = "roi_shape";
                RoiShapeId = roi_shape_id;
                Path = path;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"roi_shape_id", RoiShapeId},
            {"path", Path},
        };
                return dict;
            }
        }

        public class ScribalFontType : OwnedTableTemplate
        {
            public uint? ScribalFontTypeId { get; set; }
            public string FontName { get; set; }

            public ScribalFontType(uint? scribal_font_type_id, string font_name)
            {
                TableName = "scribal_font_type";
                OwnerTable = "scribal_font_type_owner";
                PrimaryKey = "scribal_font_type_id";
                ScribalFontTypeId = scribal_font_type_id;
                FontName = font_name;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scribal_font_type_id", ScribalFontTypeId},
            {"font_name", FontName},
        };
                return dict;
            }
        }

        public class ScribalFontTypeOwner : OwnerTableTemplate
        {
            public uint? ScribalFontTypeId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ScribalFontTypeOwner(uint? scribal_font_type_id, uint? scroll_version_id)
            {
                TableName = "scribal_font_type_owner";
                OwnedTable = "scribal_font_type";
                ScribalFontTypeId = scribal_font_type_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scribal_font_type_id", ScribalFontTypeId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class Scribe : OwnedTableTemplate
        {
            public uint? ScribeId { get; set; }
            public string Description { get; set; }

            public Scribe(uint? scribe_id, string description)
            {
                TableName = "scribe";
                OwnerTable = "scribe_owner";
                PrimaryKey = "scribe_id";
                ScribeId = scribe_id;
                Description = description;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scribe_id", ScribeId},
            {"description", Description},
        };
                return dict;
            }
        }

        public class ScribeOwner : OwnerTableTemplate
        {
            public uint? ScribeId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ScribeOwner(uint? scribe_id, uint? scroll_version_id)
            {
                TableName = "scribe_owner";
                OwnedTable = "scribe";
                ScribeId = scribe_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scribe_id", ScribeId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class Scroll : NonEditableTableTemplate
        {
            public uint? ScrollId { get; set; }

            public Scroll(uint? scroll_id)
            {
                TableName = "scroll";
                ScrollId = scroll_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_id", ScrollId},
        };
                return dict;
            }
        }

        public class ScrollData : OwnedTableTemplate
        {
            public uint? ScrollDataId { get; set; }
            public uint? ScrollId { get; set; }
            public string Name { get; set; }

            public ScrollData(uint? scroll_data_id, uint? scroll_id, string name)
            {
                TableName = "scroll_data";
                OwnerTable = "scroll_data_owner";
                PrimaryKey = "scroll_data_id";
                ScrollDataId = scroll_data_id;
                ScrollId = scroll_id;
                Name = name;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_data_id", ScrollDataId},
            {"scroll_id", ScrollId},
            {"name", Name},
        };
                return dict;
            }
        }

        public class ScrollDataOwner : OwnerTableTemplate
        {
            public uint? ScrollDataId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ScrollDataOwner(uint? scroll_data_id, uint? scroll_version_id)
            {
                TableName = "scroll_data_owner";
                OwnedTable = "scroll_data";
                ScrollDataId = scroll_data_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_data_id", ScrollDataId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ScrollToCol : OwnedTableTemplate
        {
            public uint? ScrollToColId { get; set; }
            public uint? ScrollId { get; set; }
            public uint? ColId { get; set; }

            public ScrollToCol(uint? scroll_to_col_id, uint? scroll_id, uint? col_id)
            {
                TableName = "scroll_to_col";
                OwnerTable = "scroll_to_col_owner";
                PrimaryKey = "scroll_to_col_id";
                ScrollToColId = scroll_to_col_id;
                ScrollId = scroll_id;
                ColId = col_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_to_col_id", ScrollToColId},
            {"scroll_id", ScrollId},
            {"col_id", ColId},
        };
                return dict;
            }
        }

        public class ScrollToColOwner : OwnerTableTemplate
        {
            public uint? ScrollToColId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public ScrollToColOwner(uint? scroll_to_col_id, uint? scroll_version_id)
            {
                TableName = "scroll_to_col_owner";
                OwnedTable = "scroll_to_col";
                ScrollToColId = scroll_to_col_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_to_col_id", ScrollToColId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class ScrollVersion : NonEditableTableTemplate
        {
            public uint? ScrollVersionId { get; set; }
            public ushort? UserId { get; set; }
            public uint? ScrollVersionGroupId { get; set; }
            public bool? MayWrite { get; set; }
            public bool? MayLock { get; set; }

            public ScrollVersion(uint? scroll_version_id, ushort? user_id, uint? scroll_version_group_id, bool? may_write, bool? may_lock)
            {
                TableName = "scroll_version";
                ScrollVersionId = scroll_version_id;
                UserId = user_id;
                ScrollVersionGroupId = scroll_version_group_id;
                MayWrite = may_write;
                MayLock = may_lock;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_version_id", ScrollVersionId},
            {"user_id", UserId},
            {"scroll_version_group_id", ScrollVersionGroupId},
            {"may_write", MayWrite},
            {"may_lock", MayLock},
        };
                return dict;
            }
        }

        public class ScrollVersionGroup : NonEditableTableTemplate
        {
            public uint? ScrollVersionGroupId { get; set; }
            public uint? ScrollId { get; set; }
            public bool? Locked { get; set; }

            public ScrollVersionGroup(uint? scroll_version_group_id, uint? scroll_id, bool? locked)
            {
                TableName = "scroll_version_group";
                ScrollVersionGroupId = scroll_version_group_id;
                ScrollId = scroll_id;
                Locked = locked;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_version_group_id", ScrollVersionGroupId},
            {"scroll_id", ScrollId},
            {"locked", Locked},
        };
                return dict;
            }
        }

        public class ScrollVersionGroupAdmin : NonEditableTableTemplate
        {
            public uint? ScrollVersionGroupId { get; set; }
            public ushort? UserId { get; set; }

            public ScrollVersionGroupAdmin(uint? scroll_version_group_id, ushort? user_id)
            {
                TableName = "scroll_version_group_admin";
                ScrollVersionGroupId = scroll_version_group_id;
                UserId = user_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"scroll_version_group_id", ScrollVersionGroupId},
            {"user_id", UserId},
        };
                return dict;
            }
        }

        public class Sign : NonEditableTableTemplate
        {
            public uint? SignId { get; set; }

            public Sign(uint? sign_id)
            {
                TableName = "sign";
                SignId = sign_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_id", SignId},
        };
                return dict;
            }
        }

        public class SignChar : NonEditableTableTemplate
        {
            public uint? SignCharId { get; set; }
            public uint? SignId { get; set; }
            public bool? IsVariant { get; set; }
            public string Sign { get; set; }

            public SignChar(uint? sign_char_id, uint? sign_id, bool? is_variant, string sign)
            {
                TableName = "sign_char";
                SignCharId = sign_char_id;
                SignId = sign_id;
                IsVariant = is_variant;
                Sign = sign;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_id", SignCharId},
            {"sign_id", SignId},
            {"is_variant", IsVariant},
            {"sign", Sign},
        };
                return dict;
            }
        }

        public class SignCharAttribute : OwnedTableTemplate
        {
            public uint? SignCharAttributeId { get; set; }
            public uint? SignCharId { get; set; }
            public uint? AttributeValueId { get; set; }
            public bool? Sequence { get; set; }

            public SignCharAttribute(uint? sign_char_attribute_id, uint? sign_char_id, uint? attribute_value_id, bool? sequence)
            {
                TableName = "sign_char_attribute";
                OwnerTable = "sign_char_attribute_owner";
                PrimaryKey = "sign_char_attribute_id";
                SignCharAttributeId = sign_char_attribute_id;
                SignCharId = sign_char_id;
                AttributeValueId = attribute_value_id;
                Sequence = sequence;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_attribute_id", SignCharAttributeId},
            {"sign_char_id", SignCharId},
            {"attribute_value_id", AttributeValueId},
            {"sequence", Sequence},
        };
                return dict;
            }
        }

        public class SignCharAttributeOwner : OwnerTableTemplate
        {
            public uint? SignCharAttributeId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public SignCharAttributeOwner(uint? sign_char_attribute_id, uint? scroll_version_id)
            {
                TableName = "sign_char_attribute_owner";
                OwnedTable = "sign_char_attribute";
                SignCharAttributeId = sign_char_attribute_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_attribute_id", SignCharAttributeId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class SignCharCommentary : OwnedTableTemplate
        {
            public uint? SignCharCommentaryId { get; set; }
            public uint? SignCharId { get; set; }
            public uint? AttributeId { get; set; }
            public string Commentary { get; set; }

            public SignCharCommentary(uint? sign_char_commentary_id, uint? sign_char_id, uint? attribute_id, string commentary)
            {
                TableName = "sign_char_commentary";
                OwnerTable = "sign_char_commentary_owner";
                PrimaryKey = "sign_char_commentary_id";
                SignCharCommentaryId = sign_char_commentary_id;
                SignCharId = sign_char_id;
                AttributeId = attribute_id;
                Commentary = commentary;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_commentary_id", SignCharCommentaryId},
            {"sign_char_id", SignCharId},
            {"attribute_id", AttributeId},
            {"commentary", Commentary},
        };
                return dict;
            }
        }

        public class SignCharCommentaryOwner : OwnerTableTemplate
        {
            public uint? SignCharCommentaryId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public SignCharCommentaryOwner(uint? sign_char_commentary_id, uint? scroll_version_id)
            {
                TableName = "sign_char_commentary_owner";
                OwnedTable = "sign_char_commentary";
                SignCharCommentaryId = sign_char_commentary_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_commentary_id", SignCharCommentaryId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class SignCharRoi : OwnedTableTemplate
        {
            public uint? SignCharRoiId { get; set; }
            public uint? SignCharId { get; set; }
            public uint? RoiShapeId { get; set; }
            public uint? RoiPositionId { get; set; }
            public bool? ValuesSet { get; set; }
            public bool? Exceptional { get; set; }

            public SignCharRoi(uint? sign_char_roi_id, uint? sign_char_id, uint? roi_shape_id, uint? roi_position_id, bool? values_set, bool? exceptional)
            {
                TableName = "sign_char_roi";
                OwnerTable = "sign_char_roi_owner";
                PrimaryKey = "sign_char_roi_id";
                SignCharRoiId = sign_char_roi_id;
                SignCharId = sign_char_id;
                RoiShapeId = roi_shape_id;
                RoiPositionId = roi_position_id;
                ValuesSet = values_set;
                Exceptional = exceptional;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_roi_id", SignCharRoiId},
            {"sign_char_id", SignCharId},
            {"roi_shape_id", RoiShapeId},
            {"roi_position_id", RoiPositionId},
            {"values_set", ValuesSet},
            {"exceptional", Exceptional},
        };
                return dict;
            }
        }

        public class SignCharRoiOwner : OwnerTableTemplate
        {
            public uint? SignCharRoiId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public SignCharRoiOwner(uint? sign_char_roi_id, uint? scroll_version_id)
            {
                TableName = "sign_char_roi_owner";
                OwnedTable = "sign_char_roi";
                SignCharRoiId = sign_char_roi_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sign_char_roi_id", SignCharRoiId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }

        public class SingleAction : NonEditableTableTemplate
        {
            public ulong? SingleActionId { get; set; }
            public uint? MainActionId { get; set; }
            public string Table { get; set; }
            public uint? IdInTable { get; set; }

            public SingleAction(ulong? single_action_id, uint? main_action_id, string table, uint? id_in_table)
            {
                TableName = "single_action";
                SingleActionId = single_action_id;
                MainActionId = main_action_id;
                Table = table;
                IdInTable = id_in_table;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"single_action_id", SingleActionId},
            {"main_action_id", MainActionId},
            {"table", Table},
            {"id_in_table", IdInTable},
        };
                return dict;
            }
        }

        public class SqeSession : NonEditableTableTemplate
        {
            public string SqeSessionId { get; set; }
            public ushort? UserId { get; set; }
            public uint? ScrollVersionId { get; set; }
            public DateTime? SessionStart { get; set; }
            public DateTime? LastInternalSessionEnd { get; set; }
            public string Attributes { get; set; }

            public SqeSession(string sqe_session_id, ushort? user_id, uint? scroll_version_id, DateTime? session_start, DateTime? last_internal_session_end, string attributes)
            {
                TableName = "sqe_session";
                SqeSessionId = sqe_session_id;
                UserId = user_id;
                ScrollVersionId = scroll_version_id;
                SessionStart = session_start;
                LastInternalSessionEnd = last_internal_session_end;
                Attributes = attributes;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"sqe_session_id", SqeSessionId},
            {"user_id", UserId},
            {"scroll_version_id", ScrollVersionId},
            {"session_start", SessionStart},
            {"last_internal_session_end", LastInternalSessionEnd},
            {"attributes", Attributes},
        };
                return dict;
            }
        }

        public class User : NonEditableTableTemplate
        {
            public ushort? UserId { get; set; }
            public string UserName { get; set; }
            public string Pw { get; set; }
            public string Forename { get; set; }
            public string Surname { get; set; }
            public string Organization { get; set; }
            public string Email { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public string Settings { get; set; }
            public int? LastScrollVersionId { get; set; }

            public User(ushort? user_id, string user_name, string pw, string forename, string surname, string organization, string email, DateTime? registration_date, string settings, int? last_scroll_version_id)
            {
                TableName = "user";
                UserId = user_id;
                UserName = user_name;
                Pw = pw;
                Forename = forename;
                Surname = surname;
                Organization = organization;
                Email = email;
                RegistrationDate = registration_date;
                Settings = settings;
                LastScrollVersionId = last_scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"user_id", UserId},
            {"user_name", UserName},
            {"pw", Pw},
            {"forename", Forename},
            {"surname", Surname},
            {"organization", Organization},
            {"email", Email},
            {"registration_date", RegistrationDate},
            {"settings", Settings},
            {"last_scroll_version_id", LastScrollVersionId},
        };
                return dict;
            }
        }

        public class UserComment : NonEditableTableTemplate
        {
            public int? CommentId { get; set; }
            public ushort? UserId { get; set; }
            public string CommentText { get; set; }
            public DateTime? EntryTime { get; set; }

            public UserComment(int? comment_id, ushort? user_id, string comment_text, DateTime? entry_time)
            {
                TableName = "user_comment";
                CommentId = comment_id;
                UserId = user_id;
                CommentText = comment_text;
                EntryTime = entry_time;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"comment_id", CommentId},
            {"user_id", UserId},
            {"comment_text", CommentText},
            {"entry_time", EntryTime},
        };
                return dict;
            }
        }

        public class UserContributions : NonEditableTableTemplate
        {
            public uint? ContributionId { get; set; }
            public short? UserId { get; set; }
            public string Contribution { get; set; }
            public DateTime? EntryTime { get; set; }

            public UserContributions(uint? contribution_id, short? user_id, string contribution, DateTime? entry_time)
            {
                TableName = "user_contributions";
                ContributionId = contribution_id;
                UserId = user_id;
                Contribution = contribution;
                EntryTime = entry_time;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"contribution_id", ContributionId},
            {"user_id", UserId},
            {"contribution", Contribution},
            {"entry_time", EntryTime},
        };
                return dict;
            }
        }

        public class UserSessions : NonEditableTableTemplate
        {
            public int? SessionId { get; set; }
            public ushort? UserId { get; set; }
            public string SessionKey { get; set; }
            public DateTime? SessionStart { get; set; }
            public DateTime? SessionEnd { get; set; }
            public bool? Current { get; set; }

            public UserSessions(int? session_id, ushort? user_id, string session_key, DateTime? session_start, DateTime? session_end, bool? current)
            {
                TableName = "user_sessions";
                SessionId = session_id;
                UserId = user_id;
                SessionKey = session_key;
                SessionStart = session_start;
                SessionEnd = session_end;
                Current = current;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"session_id", SessionId},
            {"user_id", UserId},
            {"session_key", SessionKey},
            {"session_start", SessionStart},
            {"session_end", SessionEnd},
            {"current", Current},
        };
                return dict;
            }
        }

        public class Word : OwnedTableTemplate
        {
            public uint? WordId { get; set; }
            public uint? QwbWordId { get; set; }

            public Word(uint? word_id, uint? qwb_word_id)
            {
                TableName = "word";
                OwnerTable = "word_owner";
                PrimaryKey = "word_id";
                WordId = word_id;
                QwbWordId = qwb_word_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"word_id", WordId},
            {"qwb_word_id", QwbWordId},
        };
                return dict;
            }
        }

        public class WordOwner : OwnerTableTemplate
        {
            public uint? WordId { get; set; }
            public uint? ScrollVersionId { get; set; }

            public WordOwner(uint? word_id, uint? scroll_version_id)
            {
                TableName = "word_owner";
                OwnedTable = "word";
                WordId = word_id;
                ScrollVersionId = scroll_version_id;
            }

            public override ListDictionary ColumsAndValues()
            {
                ListDictionary dict = new ListDictionary {
            {"word_id", WordId},
            {"scroll_version_id", ScrollVersionId},
        };
                return dict;
            }
        }
        #endregion
    }
}