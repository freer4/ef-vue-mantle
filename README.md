# EfVueMantle
A package connecting Entity Framework Core data to EfVueCrust

What this project aims to do, ideally, is allow data models from .net applications to be understood and interacted with by Vue3 applications. 

## At this stage, nothing is sacred, any updates might be breaking. 

> If you're finding this already somehow, please know this is a very incomplete restart of a thing I've rebuilt a couple of times, this will hopefully be a clean, clear, trimmed down version of that system. If this message is here, consider it an Alpha version

## Stack

### Entity Framework Core
Define your models as you do today, using EfVueMantle.ModelBase as your base class. [(see caveats)](#caveats)

### EfVueMantle 

[Get it on GitHub](https://github.com/freer4/ef-vue-mantle) or [Nuget.org](https://www.nuget.org/packages/EfVueMantle)

Mantle provides bases for model, controller, and service for each data type. This scaffolds the [basic functionality](#functionality) allowing Crust to explore data via convention. It also crafts Javascript class files for Crust, allowing your Vue3 application to understand your entire data structure.

### ef-vue-crust

[Get it on GitHub](https://github.com/freer4/ef-vue-crust) or `npm install ef-vue-crust`

Provides interfaces for Vue3 to interact with your defined data models via convention. 

Creates a virtual "Database" that holds onto records, remembers query results and sort orders, and generally lets you worry about presentation instead of how to transfer data back and forth.

### Vue3
Traverse properties in your Vue3 components with dot notation object accessors, and let ef-vue-crust worry about asyncronous data loading.  

(Core, Mantle, Crust, get it? Great. Naming things is hard.)


## Functionality {#functionality}
These generic methods are understood and automatically sought by ef-vue-crust.

### Get list of all ids for a model
Implemented

### Get list of ids fitting a pattern for a model
Implemented (case-insensitive exact match, case-insensitive string-include match)

### Get list of ids ordered by property
Implemented (descending and ascending)

### Get all records for a model
Implemented

### Get a record by id for a model
Implemented

### Get records from a list of ids for a model
Implemented

### Create new data record for model
Implemented

### Update data record for model
Not implemented

### Delete data record for model
Not implemented

## Caveats
1. The .net-> js translator currently requires that you manually define the ForeignKeys in your models. Not a huge thing but a bit annoying. This can hopefully be overcome later

2. Many-to- relationships are a little wonky. Records shouldn't return related data, only related ids. Currently, this over-pulls from the database, creates the id lists at the model level with some extra code I'd prefer wasn't necessary, and also shoves all of the related data back to the front-end. This WILL need to be fixed up at some point, but I've not gotten to that yet.

3. Although Crust supports guid PKs/ids (untested) this package defaults ids to ints. It's not a priority for me at this time, but the intent is for GUID ids to be supported

## Getting started!

You'll need to call the ExportDataObjects method, with a path to wherever you want the exported JS to live. I suggest a directory in your VueProject/src folder named something snazzy and original like "data". It will create sub-directories for models and enums as necessary.

This will automatically pick up any models using EfVueMantle.ModelBase as a base class, and any enums used in their properties.

I just drop this in my `OnModelCreating`:

```
if (_hostingEnvironment.IsDevelopment())
{
    ModelExport.ExportDataObjects("../../your-vue-project/src/data");
}
```

## Models

The `ModelBase` class contains only three properties - Id, Created, and Updated. It's mostly necessary so that the other base classes can rely on the Id property, and so the exporter knows which models to roll out. 

You will need to manually define FKs at the moment... hopefully that won't remain a thing. 

```
public int UserId { get; set; }
[ForeignKey("UserId")]
public UserProfileModel User { get; set; }
```

If you have a many-to- relationship, you'll need this in your model:

```
private List<int> _commentsIds = new();
[NotMapped]
public List<int> CommentsIds
{
    get { return Comments.Count > 0 ? Comments.Select(x => x.Id).ToList() : _commentsIds; }
    set { _commentsIds = value; }
}
public List<CommentModel> Comments { get; set; } = new();
```
And you'll need to auto-include the relationship in your db context: 

```
builder.Entity<DiscussionModel>()
    .HasMany(x => x.Comments);
builder.Entity<DiscussionModel>()
    .Navigation(x => x.Comments).AutoInclude();
```

*HELP* If anyone is proficient with EF Core, this shouldn't need all of this code. I'm looking for a way to auto-include just the FKs, i.e. CommentsIds above. In this framework, we only ever want the ids of related data. The front end ([ef-vue-crust](https://github.com/freer4/ef-vue-crust)) asks for record data by individual ids as needed. This is currenlty way, way over-pulling and over-returning. 

## Controllers

Do your controllers as usual, just use the EfVueMantle.ControllerBase class as your base. 
```
public class CommentController : ControllerBase<CommentModel, CommentService>
{
    public CommentService _commentService;
    public CommentController(CommentService commentService) : base(commentService)
    {
        _commentService = commentService;
    }
}
```

This adds the default endpoints needed for Crust to get the data it wants, and you don't have to do anything else unless you need custom functionality. 


## Services

Just like Models and Controllers, use ServiceBase to add a service with all the basic functionality needed already set up.

```
public class CommentService : ServiceBase<CommentModel>
{
    private readonly YouDbContext _context;
    private readonly DbSet<CommentModel> _comments;

    public CommentService(YouDbContext context) : base(context.Comments)
    {
        _context = context;
        _comments = context.Comments;
    }
}

```

Once again, that's all you need. Expand it as you like. 

## Not implemented features

- Open socket data pushing