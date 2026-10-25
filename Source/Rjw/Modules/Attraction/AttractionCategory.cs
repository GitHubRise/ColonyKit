#nullable disable

using System;

namespace rjw.Modules.Attraction
{
	/// <summary>
	/// <para>吸引力请求中两个小人之间的交互
	/// 如何分类。</para>
	/// <para>此处用“human”略有不当；它会包含
	/// 工具使用者智能的小人，而 RJW 实际上并不作区分。</para>
	/// <para>如果这一点很重要，请测试小人是否为类人。</para>
	/// </summary>
	public enum AttractionCategory
	{
		/// <summary>
		/// 双方都是类人（或工具使用者）。
		/// </summary>
		BetweenHumans,
		/// <summary>
		/// 双方都是动物。
		/// </summary>
		BetweenAnimals,
		/// <summary>
		/// 观察方小人是动物，而目标是人类。
		/// </summary>
		AnimalToHuman,
		/// <summary>
		/// 观察方小人是人类，而目标是动物。
		/// </summary>
		HumanToAnimal
	}

	public static class AttractionCategoryExtensions
	{
		/// <summary>
		/// 用于判断该分类是否涉及兽交的便捷扩展方法。
		/// </summary>
		/// <param name="category">要检查的分类。</param>
		/// <returns>该分类是否意味着兽交。</returns>
		public static bool IsBestiality(this AttractionCategory category) =>
			category is AttractionCategory.AnimalToHuman or AttractionCategory.HumanToAnimal;

		/// <summary>
		/// 用于判断该分类是否涉及动物的便捷扩展方法。
		/// </summary>
		/// <param name="category">要检查的分类。</param>
		/// <returns>该分类是否涉及动物。</returns>
		public static bool InvolvesAnimals(this AttractionCategory category) =>
			category is not AttractionCategory.BetweenHumans;

		/// <summary>
		/// 用于判断该分类的观察方是否为动物的便捷扩展方法。
		/// </summary>
		/// <param name="category">要检查的分类。</param>
		/// <returns>观察方是否为动物。</returns>
		public static bool AnimalObserver(this AttractionCategory category) =>
			category is AttractionCategory.BetweenAnimals or AttractionCategory.AnimalToHuman;

		/// <summary>
		/// 用于判断该分类的目标是否为动物的便捷扩展方法。
		/// </summary>
		/// <param name="category">要检查的分类。</param>
		/// <returns>目标是否为动物。</returns>
		public static bool AnimalTarget(this AttractionCategory category) =>
			category is AttractionCategory.BetweenAnimals or AttractionCategory.HumanToAnimal;
	}
}
