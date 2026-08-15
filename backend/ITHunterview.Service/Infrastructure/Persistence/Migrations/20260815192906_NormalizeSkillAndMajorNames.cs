using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSkillAndMajorNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE skills 
                SET normalized_name = regexp_replace(regexp_replace(lower(trim(name)), '[^\w\s\+#\-\.\/&]', '', 'g'), '\s+', ' ', 'g')
                WHERE name IS NOT NULL;

                UPDATE skill_aliases 
                SET normalized_alias_name = regexp_replace(regexp_replace(lower(trim(alias_name)), '[^\w\s\+#\-\.\/&]', '', 'g'), '\s+', ' ', 'g')
                WHERE alias_name IS NOT NULL;

                UPDATE majors 
                SET normalized_name = regexp_replace(regexp_replace(lower(trim(name)), '[^\w\s\+#\-\.\/&]', '', 'g'), '\s+', ' ', 'g')
                WHERE name IS NOT NULL;
            ");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

