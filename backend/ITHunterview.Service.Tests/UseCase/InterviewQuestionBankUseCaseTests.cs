using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.InterviewQuestionBank;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.UseCase;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    /// <summary>
    /// Unit Tests for InterviewQuestionBankUseCase
    /// Test ID Prefix : IQBank
    /// Function tested: GetPagedAsync(int pageIndex, int pageSize, string? industry, string? level)
    ///
    /// Test Design Table:
    /// ┌──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
    /// │          │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │
    /// │          │   01    │   02    │   03    │   04    │   05    │   06    │
    /// ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
    /// │Precond   │Repo=2   │Repo=0   │Repo=3   │Repo=2   │Repo=1   │Repo=0   │
    /// │          │ items   │ items   │ items   │ items   │ item    │ items   │
    /// ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
    /// │pageIndex │    1    │    1    │    1    │    1    │    0    │   -1    │
    /// │pageSize  │   10    │   10    │   10    │   10    │   10    │   10    │
    /// │industry  │  "DEV"  │  "DEV"  │  null   │  "DEV"  │  "DEV"  │  null   │
    /// │level     │"Junior" │"Junior" │  null   │  null   │"Junior" │  null   │
    /// ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
    /// │Return    │Paged{2} │Paged{0} │Paged{3} │Paged{2} │Paged{1} │Paged{0} │
    /// │Exception │  none   │  none   │  none   │  none   │  none   │  none   │
    /// ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
    /// │Type      │    N    │    A    │    A    │    A    │    B    │    A    │
    /// └──────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
    /// </summary>
    public class InterviewQuestionBankUseCaseTests
    {
        private readonly Mock<IInterviewQuestionBankRepository> _repositoryMock;
        private readonly InterviewQuestionBankUseCase _useCase;

        public InterviewQuestionBankUseCaseTests()
        {
            _repositoryMock = new Mock<IInterviewQuestionBankRepository>();
            _useCase = new InterviewQuestionBankUseCase(_repositoryMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helper: Build a fake PagedResult<InterviewQuestionBank>
        // ─────────────────────────────────────────────────────────────────────────
        private static PagedResult<InterviewQuestionBank> BuildPagedResult(
            List<InterviewQuestionBank> items,
            int page,
            int pageSize)
        {
            return new PagedResult<InterviewQuestionBank>
            {
                Items      = items,
                TotalCount = items.Count,
                Page       = page,
                PageSize   = pageSize
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-01 | Type: N (Normal)
        // Scenario : Valid inputs — repository returns 2 matched items.
        //            Verifies mapping of ALL 5 DTO fields and pagination metadata.
        // Precond  : Repository is set up with 2 InterviewQuestionBank entities.
        // Input    : pageIndex=1, pageSize=10, industry="DEV", level="Junior"
        // Expected : PagedResult<QuestionBankDto> with 2 items, all fields mapped correctly
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank01_ValidInputsWithData_ReturnsMappedPagedResult()
        {
            // Arrange
            int catId1 = 10;
            int catId2 = 20;
            var id1    = Guid.NewGuid();
            var id2    = Guid.NewGuid();

            var entities = new List<InterviewQuestionBank>
            {
                new() { Id = id1, CategoryId = catId1, Industry = "DEV", Level = "Junior", QuestionText = "Explain OOP?" },
                new() { Id = id2, CategoryId = catId2, Industry = "DEV", Level = "Junior", QuestionText = "What is SOLID?" }
            };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(1, 10, "DEV", "Junior"))
                .ReturnsAsync(BuildPagedResult(entities, 1, 10));

            // Act
            var result = await _useCase.GetPagedAsync(1, 10, "DEV", "Junior");

            // Assert — pagination metadata
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);

            // Assert — items count
            result.Items.Should().HaveCount(2);

            // Assert — mapping of ALL 5 DTO fields for first item
            result.Items[0].Id.Should().Be(id1);
            result.Items[0].CategoryId.Should().Be((int?)catId1);
            result.Items[0].Industry.Should().Be("DEV");
            result.Items[0].Level.Should().Be("Junior");
            result.Items[0].QuestionText.Should().Be("Explain OOP?");

            // Assert — repository was called exactly once with correct arguments
            _repositoryMock.Verify(r => r.GetPagedAsync(1, 10, "DEV", "Junior"), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-02 | Type: A (Abnormal)
        // Scenario : Valid inputs but NO matching records exist in repository.
        // Precond  : Repository returns empty Items list.
        // Input    : pageIndex=1, pageSize=10, industry="DEV", level="Junior"
        // Expected : PagedResult with empty Items list, TotalCount=0
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank02_NoMatchingData_ReturnsEmptyPagedResult()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetPagedAsync(1, 10, "DEV", "Junior"))
                .ReturnsAsync(BuildPagedResult(new List<InterviewQuestionBank>(), 1, 10));

            // Act
            var result = await _useCase.GetPagedAsync(1, 10, "DEV", "Junior");

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-03 | Type: A (Abnormal)
        // Scenario : Both industry and level are null — no filter applied.
        //            UseCase must pass null values to repository as-is (no conversion).
        // Precond  : Repository returns 3 items when called with (1, 10, null, null).
        // Input    : pageIndex=1, pageSize=10, industry=null, level=null
        // Expected : PagedResult with 3 items from all categories
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank03_NullIndustryAndLevel_PassesNullsToRepository()
        {
            // Arrange
            var entities = new List<InterviewQuestionBank>
            {
                new() { Id = Guid.NewGuid(), Industry = "DEV",  Level = "Junior", QuestionText = "Q1" },
                new() { Id = Guid.NewGuid(), Industry = "Test", Level = "Senior", QuestionText = "Q2" },
                new() { Id = Guid.NewGuid(), Industry = "BA",   Level = "Middle", QuestionText = "Q3" }
            };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(1, 10, null, null))
                .ReturnsAsync(BuildPagedResult(entities, 1, 10));

            // Act
            var result = await _useCase.GetPagedAsync(1, 10, null, null);

            // Assert
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);

            // Verify repository received exact null values (not empty strings)
            _repositoryMock.Verify(r => r.GetPagedAsync(1, 10, null, null), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-04 | Type: A (Abnormal)
        // Scenario : industry is provided but level is null — partial filter.
        //            UseCase passes the combination to repository without transformation.
        // Precond  : Repository returns 2 items for industry="DEV" with level=null.
        // Input    : pageIndex=1, pageSize=10, industry="DEV", level=null
        // Expected : PagedResult with 2 items, all having Industry="DEV"
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank04_IndustryProvidedLevelNull_FiltersOnlyByIndustry()
        {
            // Arrange
            var entities = new List<InterviewQuestionBank>
            {
                new() { Id = Guid.NewGuid(), Industry = "DEV", Level = "Junior", QuestionText = "Q1" },
                new() { Id = Guid.NewGuid(), Industry = "DEV", Level = "Senior", QuestionText = "Q2" }
            };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(1, 10, "DEV", null))
                .ReturnsAsync(BuildPagedResult(entities, 1, 10));

            // Act
            var result = await _useCase.GetPagedAsync(1, 10, "DEV", null);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items.Should().OnlyContain(i => i.Industry == "DEV");

            _repositoryMock.Verify(r => r.GetPagedAsync(1, 10, "DEV", null), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-05 | Type: B (Boundary)
        // Scenario : pageIndex = 0 — boundary value (lowest valid page index).
        //            UseCase has no guard clause; delegates 0 to repository directly.
        // Precond  : Repository is set up to handle pageIndex=0 and return 1 item.
        // Input    : pageIndex=0, pageSize=10, industry="DEV", level="Junior"
        // Expected : PagedResult returned with Page=0 — no exception thrown by UseCase
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank05_PageIndexZero_DelegatesToRepositoryWithoutException()
        {
            // Arrange
            var entities = new List<InterviewQuestionBank>
            {
                new() { Id = Guid.NewGuid(), Industry = "DEV", Level = "Junior", QuestionText = "Q1" }
            };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(0, 10, "DEV", "Junior"))
                .ReturnsAsync(BuildPagedResult(entities, 0, 10));

            // Act
            var result = await _useCase.GetPagedAsync(0, 10, "DEV", "Junior");

            // Assert — UseCase must NOT throw; boundary is handled by repository
            result.Should().NotBeNull();
            result.Page.Should().Be(0);
            result.Items.Should().HaveCount(1);

            _repositoryMock.Verify(r => r.GetPagedAsync(0, 10, "DEV", "Junior"), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-06 | Type: A (Abnormal)
        // Scenario : pageIndex = -1 — negative/invalid page index.
        //            UseCase has NO guard clause, passes -1 to repository as-is.
        // Precond  : Repository returns empty result for pageIndex=-1.
        // Input    : pageIndex=-1, pageSize=10, industry=null, level=null
        // Expected : Empty PagedResult returned — no exception from UseCase layer
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetPagedAsync_IQBank06_NegativePageIndex_PassesToRepositoryReturnsEmptyResult()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetPagedAsync(-1, 10, null, null))
                .ReturnsAsync(BuildPagedResult(new List<InterviewQuestionBank>(), -1, 10));

            // Act
            var result = await _useCase.GetPagedAsync(-1, 10, null, null);

            // Assert — UseCase does NOT throw; returns whatever repository responds
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.Page.Should().Be(-1);

            _repositoryMock.Verify(r => r.GetPagedAsync(-1, 10, null, null), Times.Once);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  GetByIdAsync(Guid id) → Task<QuestionBankDto>
        //
        //  Test Design Table:
        //  ┌──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
        //  │          │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │
        //  │          │   07    │   08    │   09    │   10    │   11    │   12    │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Precond   │Entity   │Entity   │Entity   │Entity   │Entity   │Entity   │
        //  │          │ found   │not found│not found│ found   │ found   │not found│
        //  │          │ (full)  │         │         │(nulls)  │         │         │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │id        │NewGuid  │NewGuid  │Guid.    │NewGuid  │NewGuid  │NewGuid  │
        //  │          │(exists) │(missing)│Empty    │(exists) │(exists) │(missing)│
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Return    │DTO {5   │  -      │  -      │DTO null │DTO.Id & │  -      │
        //  │          │ fields} │         │         │opt flds │QuText   │         │
        //  │Exception │  none   │KeyNFEx  │KeyNFEx  │  none   │  none   │KeyNFEx  │
        //  │          │         │"Question│"Question│         │         │"Question│
        //  │          │         │not found│not found│         │         │not found│
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Type      │    N    │    A    │    B    │    A    │    N    │    A    │
        //  └──────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
        // ═════════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-07 | Type: N (Normal)
        // Scenario : Valid Guid, entity found — verifies ALL 5 DTO fields mapped
        //            correctly and repository is called exactly once.
        // Precond  : Repository returns a fully populated entity.
        // Input    : id = Guid.NewGuid() (exists in repo)
        // Expected : QuestionBankDto with Id, CategoryId, Industry, Level, QuestionText
        //            mapped 1-to-1 from entity. Repository.GetByIdAsync called once.
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank07_EntityFound_ReturnsMappedDto()
        {
            // Arrange
            var id       = Guid.NewGuid();
            var entity   = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 42,
                Industry     = "DEV",
                Level        = "Senior",
                QuestionText = "Describe SOLID principles."
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);

            // Act
            var result = await _useCase.GetByIdAsync(id);

            // Assert — all 5 DTO fields
            result.Should().NotBeNull();
            result.Id.Should().Be(entity.Id);
            result.CategoryId.Should().Be(entity.CategoryId);
            result.Industry.Should().Be(entity.Industry);
            result.Level.Should().Be(entity.Level);
            result.QuestionText.Should().Be(entity.QuestionText);

            // Assert — repository called exactly once with the provided id
            _repositoryMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-08 | Type: A (Abnormal)
        // Scenario : Valid Guid that does NOT exist in repository.
        //            UseCase must throw KeyNotFoundException.
        // Precond  : Repository returns null for the given id.
        // Input    : id = Guid.NewGuid() (NOT in repo)
        // Expected : Throws KeyNotFoundException with message "Question not found"
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank08_EntityNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.GetByIdAsync(id);

            // Assert — exception type
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-09 | Type: B (Boundary)
        // Scenario : id = Guid.Empty — boundary value (all-zero Guid).
        //            UseCase has no guard on Guid.Empty; passes it to repository.
        //            Repository returns null → throws KeyNotFoundException.
        // Precond  : Repository returns null for Guid.Empty.
        // Input    : id = Guid.Empty
        // Expected : Throws KeyNotFoundException with exact message "Question not found"
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank09_GuidEmpty_ThrowsKeyNotFoundWithExactMessage()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.GetByIdAsync(Guid.Empty);

            // Assert — exception type AND exact message
            await act.Should()
                     .ThrowAsync<KeyNotFoundException>()
                     .WithMessage("Question not found");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-10 | Type: A (Abnormal)
        // Scenario : Entity found but all OPTIONAL fields are null
        //            (CategoryId = null, Industry = null, Level = null).
        //            UseCase must NOT throw — null values should be preserved in DTO.
        // Precond  : Repository returns entity where nullable fields are null.
        // Input    : id = Guid.NewGuid() (exists with null optional fields)
        // Expected : QuestionBankDto returned without exception; nullable fields = null
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank10_EntityWithNullOptionalFields_ReturnsDtoWithoutException()
        {
            // Arrange
            var id     = Guid.NewGuid();
            var entity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = null,   // nullable → null
                Industry     = null,   // nullable → null
                Level        = null,   // nullable → null
                QuestionText = "What is REST?"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);

            // Act
            var result = await _useCase.GetByIdAsync(id);

            // Assert — no exception, nullable fields correctly preserved as null
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.CategoryId.Should().BeNull();
            result.Industry.Should().BeNull();
            result.Level.Should().BeNull();
            result.QuestionText.Should().Be("What is REST?");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-11 | Type: N (Normal)
        // Scenario : Entity found — verify QuestionText is NOT altered (no Trim,
        //            no encoding) and Id is mapped exactly (no Guid transformation).
        // Precond  : Repository returns entity with leading/trailing spaces in text.
        // Input    : id = Guid.NewGuid() (exists)
        // Expected : DTO.QuestionText == entity.QuestionText (raw, unmodified)
        //            DTO.Id == entity.Id (exact Guid preserved)
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank11_EntityFound_QuestionTextAndIdNotAltered()
        {
            // Arrange
            var id             = Guid.NewGuid();
            var rawText        = "  Explain async/await with examples.  "; // intentional spaces
            var entity         = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 5,
                Industry     = "DEV",
                Level        = "Middle",
                QuestionText = rawText
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);

            // Act
            var result = await _useCase.GetByIdAsync(id);

            // Assert — UseCase's MapToDto must NOT modify QuestionText or Id
            result.Id.Should().Be(id);
            result.QuestionText.Should().Be(rawText); // spaces preserved
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-12 | Type: A (Abnormal)
        // Scenario : Verify exception message is EXACTLY "Question not found"
        //            (case-sensitive, no extra characters).
        // Precond  : Repository returns null for the given id.
        // Input    : id = Guid.NewGuid() (NOT in repo)
        // Expected : KeyNotFoundException.Message == "Question not found"
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByIdAsync_IQBank12_EntityNotFound_ExceptionMessageIsExact()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.GetByIdAsync(id);

            // Assert — exact exception message (case-sensitive)
            var ex = await act.Should()
                              .ThrowAsync<KeyNotFoundException>();
            ex.WithMessage("Question not found");
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  CreateAsync(CreateQuestionBankDto dto, Guid userId) → Task<QuestionBankDto>
        //
        //  Logic:
        //    1. New entity created: Id=Guid.NewGuid(), fields from dto, CreatedBy=UpdatedBy=userId
        //    2. _repository.AddAsync(entity) called
        //    3. Returns MapToDto(entity)
        //
        //  Test Design Table:
        //  ┌──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
        //  │          │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │
        //  │          │   13    │   14    │   15    │   16    │   17    │   18    │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Precond   │AddAsync │AddAsync │AddAsync │AddAsync │AddAsync │AddAsync │
        //  │          │succeeds │succeeds │succeeds │succeeds │succeeds │succeeds │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │dto.Cat   │   42    │   42    │  null   │   42    │   42    │   42    │
        //  │dto.Ind   │  "DEV"  │  "DEV"  │  null   │  "DEV"  │  "DEV"  │  "DEV"  │
        //  │dto.Level │"Junior" │"Junior" │"Junior" │"Junior" │"Junior" │"Junior" │
        //  │dto.QText │ "Q..." │ "Q..." │ "Q..." │ "Q..." │ "Q..." │ "Q..." │
        //  │userId    │NewGuid  │NewGuid  │NewGuid  │NewGuid  │Guid.    │NewGuid  │
        //  │          │         │         │         │         │Empty    │         │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Focus     │DTO all  │AddAsync │null opt │Id auto- │Guid.    │Created  │
        //  │          │5 fields │called×1 │fields ok│generated│Empty ok │By=Upd   │
        //  │          │mapped   │         │         │≠ Empty  │         │By=userId│
        //  │Return    │DTO{5f}  │DTO{5f}  │DTO{null}│DTO.Id≠∅ │DTO ok   │DTO ok   │
        //  │Exception │  none   │  none   │  none   │  none   │  none   │  none   │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Type      │    N    │    N    │    A    │    N    │    B    │    A    │
        //  └──────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
        // ═════════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-13 | Type: N (Normal)
        // Scenario : Valid dto + valid userId — verify ALL 5 DTO fields mapped
        //            correctly from dto (Id comes from entity, not dto).
        // Precond  : AddAsync completes successfully.
        // Input    : dto = { CategoryId=42, Industry="DEV", Level="Junior",
        //                    QuestionText="Explain SOLID?" }, userId = NewGuid
        // Expected : Returned QuestionBankDto has CategoryId=42, Industry="DEV",
        //            Level="Junior", QuestionText="Explain SOLID?", Id != Guid.Empty
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank13_ValidDto_ReturnsMappedDtoWithAllFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = 42,
                Industry     = "DEV",
                Level        = "Junior",
                QuestionText = "Explain SOLID?"
            };

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.CreateAsync(dto, userId);

            // Assert — all 5 DTO fields
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);        // auto-generated, never empty
            result.CategoryId.Should().Be(42);
            result.Industry.Should().Be("DEV");
            result.Level.Should().Be("Junior");
            result.QuestionText.Should().Be("Explain SOLID?");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-14 | Type: N (Normal)
        // Scenario : Verify _repository.AddAsync is called EXACTLY ONCE with an
        //            entity that carries the correct dto fields and userId.
        // Precond  : AddAsync completes successfully.
        // Input    : dto = { CategoryId=10, Industry="Test", Level="Senior",
        //                    QuestionText="What is CI/CD?" }, userId = NewGuid
        // Expected : AddAsync called once; entity passed to AddAsync has CategoryId=10,
        //            Industry="Test", Level="Senior", CreatedBy=userId, UpdatedBy=userId
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank14_AddAsyncCalledOnceWithCorrectEntity()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = 10,
                Industry     = "Test",
                Level        = "Senior",
                QuestionText = "What is CI/CD?"
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.CreateAsync(dto, userId);

            // Assert — repository called exactly once
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()), Times.Once);

            // Assert — entity passed to AddAsync has correct field values
            capturedEntity.Should().NotBeNull();
            capturedEntity!.CategoryId.Should().Be(10);
            capturedEntity.Industry.Should().Be("Test");
            capturedEntity.Level.Should().Be("Senior");
            capturedEntity.QuestionText.Should().Be("What is CI/CD?");
            capturedEntity.CreatedBy.Should().Be(userId);
            capturedEntity.UpdatedBy.Should().Be(userId);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-15 | Type: A (Abnormal)
        // Scenario : dto has null for optional fields (CategoryId=null, Industry=null).
        //            UseCase has NO validation → entity created with nulls, no exception.
        // Precond  : AddAsync completes successfully.
        // Input    : dto = { CategoryId=null, Industry=null, Level="Junior",
        //                    QuestionText="Q?" }, userId = NewGuid
        // Expected : QuestionBankDto returned with CategoryId=null, Industry=null
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank15_NullOptionalFields_ReturnsDtoWithoutException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = null,    // optional — allowed
                Industry     = null,    // optional — allowed
                Level        = "Junior",
                QuestionText = "What is polymorphism?"
            };

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.CreateAsync(dto, userId);

            // Assert — no exception, nullable fields preserved as null
            result.Should().NotBeNull();
            result.CategoryId.Should().BeNull();
            result.Industry.Should().BeNull();
            result.Level.Should().Be("Junior");
            result.QuestionText.Should().Be("What is polymorphism?");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-16 | Type: N (Normal)
        // Scenario : Verify entity.Id is AUTO-GENERATED (Guid.NewGuid()) — it must
        //            be a non-empty Guid not provided by the caller.
        // Precond  : AddAsync completes successfully.
        // Input    : dto = valid, userId = NewGuid
        // Expected : result.Id != Guid.Empty, and each call produces a unique Id
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank16_EntityIdIsAutoGenerated_NotEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = 1,
                Industry     = "BA",
                Level        = "Middle",
                QuestionText = "Describe the SDLC process."
            };

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act — call twice to verify distinct Ids
            var result1 = await _useCase.CreateAsync(dto, userId);
            var result2 = await _useCase.CreateAsync(dto, userId);

            // Assert — each call generates a unique non-empty Guid
            result1.Id.Should().NotBe(Guid.Empty);
            result2.Id.Should().NotBe(Guid.Empty);
            result1.Id.Should().NotBe(result2.Id); // must be distinct across calls
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-17 | Type: B (Boundary)
        // Scenario : userId = Guid.Empty — boundary value. UseCase has NO guard;
        //            entity is created with CreatedBy=Guid.Empty, UpdatedBy=Guid.Empty.
        // Precond  : AddAsync completes successfully.
        // Input    : dto = valid, userId = Guid.Empty
        // Expected : No exception thrown; entity created with CreatedBy=UpdatedBy=Guid.Empty
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank17_UserIdGuidEmpty_EntityCreatedWithEmptyUserId()
        {
            // Arrange
            var userId = Guid.Empty; // boundary value
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = 5,
                Industry     = "DEV",
                Level        = "Junior",
                QuestionText = "What is dependency injection?"
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act — UseCase must NOT throw for Guid.Empty userId
            var result = await _useCase.CreateAsync(dto, userId);

            // Assert — entity was created with Guid.Empty for both auditing fields
            result.Should().NotBeNull();
            capturedEntity.Should().NotBeNull();
            capturedEntity!.CreatedBy.Should().Be(Guid.Empty);
            capturedEntity.UpdatedBy.Should().Be(Guid.Empty);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-18 | Type: A (Abnormal)
        // Scenario : Verify BOTH CreatedBy AND UpdatedBy are set to userId
        //            (not swapped, not null, not a different value).
        // Precond  : AddAsync completes successfully.
        // Input    : dto = valid, userId = NewGuid (specific, known value)
        // Expected : capturedEntity.CreatedBy == userId AND
        //            capturedEntity.UpdatedBy == userId (same value, same field)
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task CreateAsync_IQBank18_BothCreatedByAndUpdatedBySetToUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateQuestionBankDto
            {
                CategoryId   = 7,
                Industry     = "DEV",
                Level        = "Senior",
                QuestionText = "Explain event-driven architecture."
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.CreateAsync(dto, userId);

            // Assert — CreatedBy and UpdatedBy both equal userId (not null, not swapped)
            capturedEntity.Should().NotBeNull();
            capturedEntity!.CreatedBy.Should().Be(userId);
            capturedEntity.UpdatedBy.Should().Be(userId);
            capturedEntity.CreatedBy.Should().Be(capturedEntity.UpdatedBy); // both same
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  UpdateAsync(Guid id, UpdateQuestionBankDto dto, Guid userId)
        //       → Task<QuestionBankDto>
        //
        //  Logic:
        //    1. GetByIdAsync(id) → if null: throw KeyNotFoundException("Question not found")
        //    2. Patch: entity.CategoryId, Industry, Level, QuestionText = dto values
        //    3. entity.UpdatedBy = userId
        //    4. _repository.UpdateAsync(entity) called
        //    5. Returns MapToDto(entity)
        //
        //  Test Design Table:
        //  ┌──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
        //  │          │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │
        //  │          │   19    │   20    │   21    │   22    │   23    │   24    │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Precond   │Entity   │Entity   │Entity   │Entity   │Entity   │Entity   │
        //  │          │ found   │not found│not found│ found   │ found   │ found   │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │id        │NewGuid  │NewGuid  │Guid.    │NewGuid  │NewGuid  │NewGuid  │
        //  │          │(exists) │(missing)│Empty    │(exists) │(exists) │(exists) │
        //  │dto.CatId │   99    │   99    │   99    │   99    │   99    │  null   │
        //  │dto.Ind   │"DevOps" │"DevOps" │"DevOps" │"DevOps" │"DevOps" │  null   │
        //  │dto.Level │"Senior" │"Senior" │"Senior" │"Senior" │"Senior" │"Senior" │
        //  │dto.QText │ "Q..."  │ "Q..."  │ "Q..."  │ "Q..."  │ "Q..."  │ "Q..."  │
        //  │userId    │NewGuid  │NewGuid  │NewGuid  │NewGuid  │NewGuid  │NewGuid  │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Focus     │DTO all  │KeyNFEx  │KeyNFEx  │Update   │Updated  │null opt │
        //  │          │4 fields │thrown   │Guid.    │Async×1  │By=userId│fields   │
        //  │          │updated  │         │Empty    │+entity  │         │null ok  │
        //  │Return    │DTO{4f}  │  -      │  -      │DTO ok   │DTO ok   │DTO{null}│
        //  │Exception │  none   │KeyNFEx  │KeyNFEx  │  none   │  none   │  none   │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Type      │    N    │    A    │    B    │    N    │    A    │    A    │
        //  └──────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
        // ═════════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-19 | Type: N (Normal)
        // Scenario : Entity found — verify ALL 4 dto fields are applied to entity
        //            and returned in DTO. Entity.Id must NOT change.
        // Precond  : GetByIdAsync returns existing entity; UpdateAsync succeeds.
        // Input    : id=existing, dto={Cat=99, Ind="DevOps", Lv="Senior", Q="Q?"},
        //            userId=NewGuid
        // Expected : DTO returned with updated values; entity.Id unchanged
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank19_EntityFound_ReturnsUpdatedDto()
        {
            // Arrange
            var id     = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var existingEntity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 1,          // OLD values
                Industry     = "OLD",
                Level        = "Junior",
                QuestionText = "Old question?"
            };

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = 99,
                Industry     = "DevOps",
                Level        = "Senior",
                QuestionText = "What is Kubernetes?"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.UpdateAsync(id, dto, userId);

            // Assert — all 4 dto fields reflected in DTO
            result.Should().NotBeNull();
            result.Id.Should().Be(id);              // Id must NOT change
            result.CategoryId.Should().Be(99);
            result.Industry.Should().Be("DevOps");
            result.Level.Should().Be("Senior");
            result.QuestionText.Should().Be("What is Kubernetes?");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-20 | Type: A (Abnormal)
        // Scenario : Entity with given id does NOT exist in repository.
        //            UseCase must throw KeyNotFoundException with "Question not found".
        // Precond  : GetByIdAsync returns null.
        // Input    : id = NewGuid (not in repo), dto = any, userId = NewGuid
        // Expected : KeyNotFoundException thrown; UpdateAsync NOT called
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank20_EntityNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((InterviewQuestionBank?)null);

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = 99,
                Industry     = "DevOps",
                Level        = "Senior",
                QuestionText = "Any question?"
            };

            // Act
            var act = async () => await _useCase.UpdateAsync(id, dto, Guid.NewGuid());

            // Assert — throws with exact message
            await act.Should()
                     .ThrowAsync<KeyNotFoundException>()
                     .WithMessage("Question not found");

            // Assert — UpdateAsync must NOT be called when entity is missing
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-21 | Type: B (Boundary)
        // Scenario : id = Guid.Empty — boundary value. No guard in UseCase;
        //            GetByIdAsync(Guid.Empty) returns null → throws KeyNotFoundException.
        // Precond  : GetByIdAsync returns null for Guid.Empty.
        // Input    : id = Guid.Empty, dto = any, userId = NewGuid
        // Expected : KeyNotFoundException("Question not found"); UpdateAsync NOT called
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank21_GuidEmpty_ThrowsKeyNotFoundException()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((InterviewQuestionBank?)null);

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = 1,
                Industry     = "DEV",
                Level        = "Junior",
                QuestionText = "Q?"
            };

            // Act
            var act = async () => await _useCase.UpdateAsync(Guid.Empty, dto, Guid.NewGuid());

            // Assert
            await act.Should()
                     .ThrowAsync<KeyNotFoundException>()
                     .WithMessage("Question not found");

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-22 | Type: N (Normal)
        // Scenario : Verify _repository.UpdateAsync is called EXACTLY ONCE and the
        //            entity passed to it has all 4 dto fields overwritten correctly.
        //            Uses Moq Callback to capture the mutated entity.
        // Precond  : GetByIdAsync returns existing entity; UpdateAsync succeeds.
        // Input    : id=existing, dto={Cat=99, Ind="DevOps", Lv="Senior", Q="Q?"},
        //            userId=NewGuid
        // Expected : UpdateAsync called once; capturedEntity has dto values + UpdatedBy=userId
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank22_UpdateAsyncCalledOnceWithPatchedEntity()
        {
            // Arrange
            var id     = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var existingEntity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 1,
                Industry     = "OLD",
                Level        = "Junior",
                QuestionText = "Old?"
            };

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = 99,
                Industry     = "DevOps",
                Level        = "Senior",
                QuestionText = "What is Kubernetes?"
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.UpdateAsync(id, dto, userId);

            // Assert — UpdateAsync called exactly once
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()), Times.Once);

            // Assert — entity passed to UpdateAsync has all 4 dto fields applied
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Id.Should().Be(id);               // Id unchanged
            capturedEntity.CategoryId.Should().Be(99);
            capturedEntity.Industry.Should().Be("DevOps");
            capturedEntity.Level.Should().Be("Senior");
            capturedEntity.QuestionText.Should().Be("What is Kubernetes?");
            capturedEntity.UpdatedBy.Should().Be(userId);     // audit field set
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-23 | Type: A (Abnormal)
        // Scenario : Verify entity.UpdatedBy is set to userId (not null, not CreatedBy).
        //            Captures entity via Callback and checks UpdatedBy specifically.
        // Precond  : GetByIdAsync returns entity; UpdateAsync succeeds.
        // Input    : id=existing, dto=valid, userId=known NewGuid
        // Expected : capturedEntity.UpdatedBy == userId
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank23_UpdatedBySetToUserId()
        {
            // Arrange
            var id       = Guid.NewGuid();
            var userId   = Guid.NewGuid();
            var original = Guid.NewGuid(); // original CreatedBy — must NOT be used for UpdatedBy

            var existingEntity = new InterviewQuestionBank
            {
                Id         = id,
                CreatedBy  = original,   // should remain untouched
                UpdatedBy  = original,   // will be overwritten
                Level        = "Junior",
                QuestionText = "Old?"
            };

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = 5,
                Industry     = "DEV",
                Level        = "Senior",
                QuestionText = "New question?"
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.UpdateAsync(id, dto, userId);

            // Assert — UpdatedBy = userId (overwritten); CreatedBy unchanged
            capturedEntity!.UpdatedBy.Should().Be(userId);
            capturedEntity.CreatedBy.Should().Be(original); // CreatedBy must NOT be touched
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-24 | Type: A (Abnormal)
        // Scenario : dto has null for optional fields (CategoryId=null, Industry=null).
        //            UseCase assigns null directly → entity nullable fields become null.
        // Precond  : GetByIdAsync returns entity; UpdateAsync succeeds.
        // Input    : id=existing, dto={CategoryId=null, Industry=null, Level="Senior",
        //                             QuestionText="Q?"}, userId=NewGuid
        // Expected : DTO returned with CategoryId=null, Industry=null; no exception
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateAsync_IQBank24_NullOptionalFieldsInDto_EntityUpdatedWithNulls()
        {
            // Arrange
            var id     = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var existingEntity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 10,      // will be overwritten with null
                Industry     = "DEV",   // will be overwritten with null
                Level        = "Junior",
                QuestionText = "Old?"
            };

            var dto = new UpdateQuestionBankDto
            {
                CategoryId   = null,    // clear it
                Industry     = null,    // clear it
                Level        = "Senior",
                QuestionText = "Updated question."
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(existingEntity);
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _useCase.UpdateAsync(id, dto, userId);

            // Assert — returned DTO reflects nulls
            result.Should().NotBeNull();
            result.CategoryId.Should().BeNull();
            result.Industry.Should().BeNull();
            result.Level.Should().Be("Senior");
            result.QuestionText.Should().Be("Updated question.");

            // Assert — entity in repo also has nulls
            capturedEntity!.CategoryId.Should().BeNull();
            capturedEntity.Industry.Should().BeNull();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  DeleteAsync(Guid id) → Task (void)
        //
        //  Logic:
        //    1. GetByIdAsync(id) → if null: throw KeyNotFoundException("Question not found")
        //    2. _repository.DeleteAsync(entity) called
        //    3. Returns void
        //
        //  Test Design Table:
        //  ┌──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
        //  │          │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │ IQBank  │
        //  │          │   25    │   26    │   27    │   28    │   29    │   30    │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Precond   │Entity   │Entity   │Entity   │Entity   │Entity   │Entity   │
        //  │          │ found   │not found│not found│ found   │not found│ found   │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │id        │NewGuid  │NewGuid  │Guid.    │NewGuid  │NewGuid  │NewGuid  │
        //  │          │(exists) │(missing)│Empty    │(exists) │(missing)│(exists) │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Focus     │Delete   │KeyNFEx  │KeyNFEx  │Delete   │Exact    │GetById  │
        //  │          │Async×1  │ thrown  │Guid.    │Async    │exception│Async×1  │
        //  │          │no throw │Del=×0   │Empty    │called   │message  │with id  │
        //  │          │         │         │Del=×0   │with same│"Quest.  │         │
        //  │          │         │         │         │entity   │not found│         │
        //  │Return    │  void   │  -      │  -      │  void   │  -      │  void   │
        //  │Exception │  none   │KeyNFEx  │KeyNFEx  │  none   │KeyNFEx  │  none   │
        //  ├──────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
        //  │Type      │    N    │    A    │    B    │    N    │    A    │    A    │
        //  └──────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
        // ═════════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-25 | Type: N (Normal)
        // Scenario : Entity found — UseCase calls DeleteAsync on repository exactly
        //            once and completes without throwing any exception.
        // Precond  : GetByIdAsync returns an existing entity; DeleteAsync succeeds.
        // Input    : id = Guid.NewGuid() (exists in repo)
        // Expected : No exception; _repository.DeleteAsync called once
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank25_EntityFound_DeletesSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 1,
                Industry     = "DEV",
                Level        = "Junior",
                QuestionText = "Q?"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);
            _repositoryMock
                .Setup(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act — must complete without exception
            var act = async () => await _useCase.DeleteAsync(id);
            await act.Should().NotThrowAsync();

            // Assert — DeleteAsync called exactly once
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-26 | Type: A (Abnormal)
        // Scenario : Entity does NOT exist — throws KeyNotFoundException.
        //            DeleteAsync must NOT be called (no double-hit on repo).
        // Precond  : GetByIdAsync returns null.
        // Input    : id = Guid.NewGuid() (NOT in repo)
        // Expected : KeyNotFoundException thrown; DeleteAsync NOT called
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank26_EntityNotFound_ThrowsKeyNotFoundAndDeleteNotCalled()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.DeleteAsync(id);

            // Assert — exception thrown
            await act.Should()
                     .ThrowAsync<KeyNotFoundException>()
                     .WithMessage("Question not found");

            // Assert — DeleteAsync must NOT be called when entity is missing
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-27 | Type: B (Boundary)
        // Scenario : id = Guid.Empty — boundary value. UseCase has no guard;
        //            GetByIdAsync(Guid.Empty) returns null → throws KeyNotFoundException.
        //            DeleteAsync must NOT be called.
        // Precond  : GetByIdAsync returns null for Guid.Empty.
        // Input    : id = Guid.Empty
        // Expected : KeyNotFoundException("Question not found"); DeleteAsync NOT called
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank27_GuidEmpty_ThrowsKeyNotFoundAndDeleteNotCalled()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.DeleteAsync(Guid.Empty);

            // Assert
            await act.Should()
                     .ThrowAsync<KeyNotFoundException>()
                     .WithMessage("Question not found");

            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-28 | Type: N (Normal)
        // Scenario : Entity found — verify DeleteAsync is called with the EXACT SAME
        //            entity instance returned by GetByIdAsync (correct reference).
        //            Uses Moq Callback to capture entity passed to DeleteAsync.
        // Precond  : GetByIdAsync returns a specific entity.
        // Input    : id = Guid.NewGuid() (exists)
        // Expected : capturedEntity.Id == id (same entity, not a different one)
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank28_DeleteAsyncCalledWithCorrectEntityReference()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new InterviewQuestionBank
            {
                Id           = id,
                CategoryId   = 5,
                Industry     = "Test",
                Level        = "Senior",
                QuestionText = "What is TDD?"
            };

            InterviewQuestionBank? capturedEntity = null;
            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);
            _repositoryMock
                .Setup(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()))
                .Callback<InterviewQuestionBank>(e => capturedEntity = e)
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.DeleteAsync(id);

            // Assert — DeleteAsync called with the exact entity from GetByIdAsync
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()), Times.Once);
            capturedEntity.Should().NotBeNull();
            capturedEntity!.Id.Should().Be(id);             // correct entity
            capturedEntity.Should().BeSameAs(entity);        // exact same reference
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-29 | Type: A (Abnormal)
        // Scenario : Verify exception message is EXACTLY "Question not found"
        //            (case-sensitive, no extra text).
        // Precond  : GetByIdAsync returns null.
        // Input    : id = Guid.NewGuid() (NOT in repo)
        // Expected : KeyNotFoundException.Message == "Question not found"
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank29_EntityNotFound_ExceptionMessageIsExact()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((InterviewQuestionBank?)null);

            // Act
            var act = async () => await _useCase.DeleteAsync(id);

            // Assert — exact exception message (case-sensitive)
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.WithMessage("Question not found");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IQBank-30 | Type: A (Abnormal)
        // Scenario : Verify GetByIdAsync is called exactly once with the provided id,
        //            confirming UseCase does NOT skip the lookup step.
        // Precond  : GetByIdAsync returns an existing entity; DeleteAsync succeeds.
        // Input    : id = Guid.NewGuid() (exists)
        // Expected : GetByIdAsync(id) called exactly once with the correct id
        // ─────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteAsync_IQBank30_GetByIdAsyncCalledOnceWithCorrectId()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new InterviewQuestionBank
            {
                Id           = id,
                Industry     = "DEV",
                Level        = "Junior",
                QuestionText = "Q?"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);
            _repositoryMock
                .Setup(r => r.DeleteAsync(It.IsAny<InterviewQuestionBank>()))
                .Returns(Task.CompletedTask);

            // Act
            await _useCase.DeleteAsync(id);

            // Assert — GetByIdAsync called exactly once with the correct Guid
            _repositoryMock.Verify(r => r.GetByIdAsync(id), Times.Once);

            // Assert — and NOT called with any other Guid
            _repositoryMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(g => g != id)), Times.Never);
        }
    }
}
