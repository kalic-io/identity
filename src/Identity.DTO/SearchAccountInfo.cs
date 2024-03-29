﻿namespace Identity.DTO
{
    using MedEasy.DTO.Search;

    public class SearchAccountInfo : AbstractSearchInfo<AccountInfo>
    {
        /// <summary>
        /// Filter to apply on <see cref="AccountInfo.Username"/> property
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Filter to apply on <see cref="AccountInfo.Email"/> property
        /// </summary>
        public string Email { get; set; }


        /// <summary>
        /// Filter to apply on <see cref="AccountInfo.Name"/> property
        /// </summary>
        public string Name { get; set; }
    }
}
