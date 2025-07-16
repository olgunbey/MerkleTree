static void MerkleTreeAlgorithm(List<User> db1, List<User> replica)
{
    var db1Nodes = db1.Select(y => new Node() { User = y, Hash = HashData(y) }).ToList();

    var replicaNodes = replica.Select(y => new Node() { User = y, Hash = HashData(y) }).ToList();

    var dbCount = db1.Count;
    var replicaCount = replica.Count;

    var dbCountToNextPowerOfTwo = CountToNextPowerOfTwo(dbCount);
    var replicaCountToNextPowerOfTwo = CountToNextPowerOfTwo(replicaCount);

    var db1LastValue = db1Nodes.Last();
    var replicaLastValue = replicaNodes.Last();
    for (int i = 1; i <= dbCountToNextPowerOfTwo; i++)
    {
        db1Nodes.Add(db1LastValue);
        replicaNodes.Add(replicaLastValue);
    }


    var nodeService = new NodeService();

    var dbDct = nodeService.BuildMerkleTree(db1Nodes);

    var replicaDct = nodeService.BuildMerkleTree(replicaNodes);


    var tuple = nodeService.Compare(dbDct, replicaDct);


    Node nodeDb = db1Nodes.Single(x => x.Hash == tuple.db1HashCode);

    Node nodeReplication = replicaNodes.Single(x => x.Hash == tuple.replicationHashCode);


    if (nodeDb.User.UpdatedDate > nodeReplication.User.UpdatedDate)
    {

        var updatedReplicaEntity = replica.Single(y => y.Id == nodeReplication.User.Id);
        updatedReplicaEntity.UpdatedDate = nodeReplication.User.UpdatedDate;
        updatedReplicaEntity.Surname=nodeReplication.User.Surname;
        updatedReplicaEntity.PhoneNumber=nodeReplication.User.PhoneNumber;
        updatedReplicaEntity.Name=nodeReplication.User.Name;
    }
    else if (nodeDb.User.UpdatedDate < nodeReplication.User.UpdatedDate)
    {
        var updatedDb1 = db1.Single(y => y.Id == nodeDb.User.Id);

        updatedDb1.UpdatedDate = nodeReplication.User.UpdatedDate;
        updatedDb1.Surname = nodeReplication.User.Surname;
        updatedDb1.PhoneNumber = nodeReplication.User.PhoneNumber;
        updatedDb1.Name = nodeReplication.User.Name;
    }



    foreach (var item in db1)
    {
        Console.WriteLine(item.Id + " " + item.Name + " " + item.PhoneNumber + " " + item.UpdatedDate);
    }
    Console.WriteLine("-----------------------------------------------------------------------------");
    Console.WriteLine("-----------------------------------------------------------------------------");
    Console.WriteLine("-----------------------------------------------------------------------------");
    foreach (var item in replica)
    {
        Console.WriteLine(item.Id + " " + item.Name + " " + item.PhoneNumber + " " + item.UpdatedDate);
    }


}
static string HashData(User user)
{
    var rawData = $"{user.Id}{user.Name}{user.Surname}{user.PhoneNumber}{user.UpdatedDate}";
    var bytes = System.Text.Encoding.UTF8.GetBytes(rawData);
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashBytes = sha256.ComputeHash(bytes);
    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    return hashString;
}

static int CountToNextPowerOfTwo(int count)
{
    int power = 1;
    while (power < count)
    {
        power *= 2;
    }
    return power - count;

}


var db1 = new List<User>
{
    new User { Id = 1, Name = "Alice", Surname = "Smith", PhoneNumber = "123-456-7890", UpdatedDate = DateTime.UtcNow },
    new User { Id = 2, Name = "Bob", Surname = "Johnson", PhoneNumber = "234-567-8901",UpdatedDate = DateTime.UtcNow },
    new User { Id = 3, Name = "Charlie", Surname = "Williams", PhoneNumber = "345-678-9012",UpdatedDate = DateTime.UtcNow },
    new User { Id = 4, Name = "Arda", Surname = "Şahin", PhoneNumber = "346-488-4012",UpdatedDate = DateTime.UtcNow }
};

var replica = new List<User>
{
    new User { Id = 1, Name = "Alice", Surname = "Smith", PhoneNumber = "123-456-7890",UpdatedDate = DateTime.UtcNow },
    new User { Id = 2, Name = "Bob", Surname = "Johnson", PhoneNumber = "234-567-8901",UpdatedDate = DateTime.UtcNow },
    new User { Id = 3, Name = "Charlie", Surname = "Williams", PhoneNumber = "345-678-9012",UpdatedDate = DateTime.UtcNow },
    new User { Id = 4, Name = "Meltem", Surname = "Şahin", PhoneNumber = "346-688-4012",UpdatedDate = DateTime.UtcNow.AddDays(1) }
};



Console.WriteLine("MasterDb");
foreach (var item in db1)
{
    Console.WriteLine(item.Id + " " + item.Name + " " + item.PhoneNumber + " " + item.UpdatedDate);
}
Console.WriteLine("-----------------------------------------------------------------------------");
Console.WriteLine("-----------------------------------------------------------------------------");
Console.WriteLine("-----------------------------------------------------------------------------");

Console.WriteLine("Replicated Db");
foreach (var item in replica)
{
    Console.WriteLine(item.Id + " " + item.Name + " " + item.PhoneNumber + " " + item.UpdatedDate);
}


Console.WriteLine("---------------------- MERKLE TREE ALGORITHM EXECUTED-------------------");
MerkleTreeAlgorithm(db1, replica);





public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime UpdatedDate { get; set; }

}


public class Node
{
    public Node Left { get; set; }
    public Node Right { get; set; }
    public string Hash { get; set; }
    public User User { get; set; }
}
public class NodeService
{
    public string ComputeHash(string left, string right)
    {
        var combined = $"{left}{right}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public Node Root { get; private set; }
    public Node BuildMerkleTree(List<Node> hashSets)
    {
        int i = 0;

        List<Node> afterLayer = new List<Node>();
        while (i < hashSets.Count)
        {
            string combinationHash = ComputeHash(hashSets[i].Hash, hashSets[i + 1].Hash);

            var newNode = new Node
            {
                Left = hashSets[i],
                Right = hashSets[i + 1],
                Hash = combinationHash
            };

            afterLayer.Add(newNode);
            i += 2;
        }
        if (afterLayer.Count == 1)
        {
            Root = afterLayer.Single();
            return Root;
        }
        else
        {
            return BuildMerkleTree(afterLayer);
        }
    }

    public (string db1HashCode, string replicationHashCode) Compare(Node rootDb1, Node rootReplica)
    {
        if (rootDb1.Left == null && rootDb1.Right == null || rootReplica.Left == null && rootReplica.Right == null)
        {
            return (rootDb1.Hash, rootReplica.Hash);
        }

        if (rootDb1.Hash != rootReplica.Hash)
        {
            if (rootDb1.Left.Hash != rootReplica.Left.Hash)
            {
                return Compare(rootDb1.Left, rootReplica.Left);
            }
            if (rootDb1.Right.Hash != rootReplica.Right.Hash)
            {
                return Compare(rootDb1.Right, rootReplica.Right);
            }
        }
        return (rootDb1.Hash, rootReplica.Hash);

    }
}
