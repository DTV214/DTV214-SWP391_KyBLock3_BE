using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

/// <summary>
/// Defines the rules for each category within a ProductConfig
/// Each ConfigDetail specifies: "This basket config requires X items from Category Y"
/// Example: "Giỏ Tết Sang Trọng" (ConfigId=5) requires:
///   - 3 items from "Bánh ngọt" (ConfigDetail: Configid=5, Categoryid=1, Quantity=3)
///   - 2 items from "Kẹo mứt" (ConfigDetail: Configid=5, Categoryid=2, Quantity=2)
///   - 1 item from "Nước uống" (ConfigDetail: Configid=5, Categoryid=3, Quantity=1)
/// When adding ProductDetail to a basket, the system validates against these rules
/// </summary>
public partial class ConfigDetail
{
    public int Configdetailid { get; set; }

    /// <summary>
    /// Reference to the ProductConfig (basket template)
    /// </summary>
    public int? Configid { get; set; } // ConfigId của giỏ Tết

    /// <summary>
    /// Reference to the ProductCategory that this rule applies to
    /// </summary>
    public int? Categoryid { get; set; }

    /// <summary>
    /// Maximum (or required) number of items from this category
    /// Validated when adding/updating ProductDetail
    /// </summary>
    public int? Quantity { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual ProductConfig? Config { get; set; }
}
