using Microsoft.AspNetCore.Mvc;

namespace EfVueMantle;

public class ControllerBase<TModel, TService> : ControllerBase<TModel, TService, long> 
    where TModel : ModelBase
    where TService : ServiceBase<TModel>
{ 
    public ControllerBase(TService service) : base(service) { }
}

[ApiController]
[Route("[controller]")]
public class ControllerBase<TModel, TService, TKey> : ControllerBase
    where TModel : ModelBase<TKey>
    where TService : ServiceBase<TModel, TKey>
    where TKey : IEquatable<TKey>
{

    private TService _service { get; set; }

    public ControllerBase(TService service)
    {
        _service = service;
    }


    [HttpGet("Get/{id}")]
    public virtual IActionResult Get(TKey id)
    {
        return Ok(_service.Get(id));
    }


    [HttpGet("All")]
    //Return all available records
    public virtual IActionResult Get()
    {
        List<TModel> list = _service.GetAll();
        return Ok(list);
    }


    //Return records from list of ids
    [HttpPost("List")]
    public virtual IActionResult GetList(List<TKey> ids)
    {
        List<TModel> list = _service.GetList(ids);
        return Ok(list);
    }

    //Return list of all available ids
    [HttpGet("AllIds")]
    public virtual IActionResult Series()
    {
        return Ok(_service.GetAllIds());
    }

    //Return list of ids based on query
    [HttpGet("Index/{type}/{prop}/{spec}")]
    public virtual IActionResult Index(string type, string prop, string spec)
    {
        var list = new List<TKey>();
        try
        {
            switch (type)
            {
                case "order":
                    list = _service.Order(prop, Int32.Parse(spec));
                    break;
                case "equals":
                    list = _service.Equals(prop, spec);
                    break;
                case "contains":
                    list = _service.Contains(prop, spec);
                    break;
                case "any":
                    list = _service.Any(prop, spec);
                    break;
                default: throw new Exception($"Unknown Index \"{type}\" on prop {prop}");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(list);
    }


    //Add instance of this model
    [HttpPost("Save")]
    public virtual IActionResult Save(TModel data)
    {
        _service.Save(data);
        return Ok(data);
    }

    //Add many instances of this model
    [HttpPost("SaveAll")]
    public virtual IActionResult SaveAll(List<TModel> datas)
    {
        _service.SaveAll(datas);
        return Ok(datas);
    }

    [HttpDelete("Delete/{id}")]
    public virtual IActionResult Delete(TKey id)
    {
        _service.Delete(id);
        return Ok(true);
    }

}

