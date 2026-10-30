using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pr1_ASP.Authorization;
using Pr1_ASP.Dtos;
using Pr1_ASP.Services;

namespace Pr1_ASP.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _service;
        private readonly IAuthorizationService _authorizationService;

        public UsersController(IUserService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll(
            int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _service.GetAllAsync(page, pageSize, cancellationToken);
            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new OwnerOrAdminRequirement());
            if (!authResult.Succeeded) return Forbid();

            var item = await _service.GetByIdAsync(id, cancellationToken);
            if (item is null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public Task<IActionResult> Create(CreateUserDto dto, CancellationToken cancellationToken) =>
            HandleAsync(async () =>
            {
                var created = await _service.CreateAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
            });

        [HttpPut("{id:int}")]
        public Task<IActionResult> Update(int id, UpdateUserDto dto, CancellationToken cancellationToken) =>
            HandleAsync(async () =>
            {
                var authResult = await _authorizationService.AuthorizeAsync(User, id, new OwnerOrAdminRequirement());
                if (!authResult.Succeeded) return Forbid();

                var updated = await _service.UpdateAsync(id, dto, cancellationToken);
                if (updated is null) return NotFound();
                return Ok(updated);
            });

        [HttpPatch("{id:int}")]
        public Task<IActionResult> Patch(int id, PatchUserDto dto, CancellationToken cancellationToken) =>
            HandleAsync(async () =>
            {
                var authResult = await _authorizationService.AuthorizeAsync(User, id, new OwnerOrAdminRequirement());
                if (!authResult.Succeeded) return Forbid();

                var patched = await _service.PatchAsync(id, dto, cancellationToken);
                if (patched is null) return NotFound();
                return Ok(patched);
            });

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            HandleAsync(async () =>
            {
                var deleted = await _service.DeleteAsync(id, cancellationToken);
                if (!deleted) return NotFound();
                return NoContent();
            });
    }
}