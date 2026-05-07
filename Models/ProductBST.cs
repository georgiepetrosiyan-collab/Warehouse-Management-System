//ProduceBST.cs

namespace WarehouseAPI.Models
{
    public class ProductNode
    {
        public Product Data;
        public ProductNode Left, Right;
        public ProductNode(Product data) { Data = data; }
    }

    public class ProductBST
    {
        private ProductNode root;

        public void Insert(Product p) => root = InsertRec(root, p);

        private ProductNode InsertRec(ProductNode node, Product p)
        {
            if (node == null) return new ProductNode(p);
            if (p.Id < node.Data.Id) node.Left  = InsertRec(node.Left,  p);
            else                     node.Right = InsertRec(node.Right, p);
            return node;
        }

        public Product Search(int id) => SearchRec(root, id);

        private Product SearchRec(ProductNode node, int id)
        {
            if (node == null) return null;
            if (node.Data.Id == id) return node.Data;
            return id < node.Data.Id
                ? SearchRec(node.Left,  id)
                : SearchRec(node.Right, id);
        }

        public void Delete(int id) => root = DeleteRec(root, id);

        private ProductNode DeleteRec(ProductNode node, int id)
        {
            if (node == null) return null;
            if      (id < node.Data.Id) node.Left  = DeleteRec(node.Left,  id);
            else if (id > node.Data.Id) node.Right = DeleteRec(node.Right, id);
            else
            {
                if (node.Left  == null) return node.Right;
                if (node.Right == null) return node.Left;
                var min = FindMin(node.Right);
                node.Data  = min.Data;
                node.Right = DeleteRec(node.Right, min.Data.Id);
            }
            return node;
        }

        private ProductNode FindMin(ProductNode node)
        {
            while (node.Left != null) node = node.Left;
            return node;
        }
    }
}