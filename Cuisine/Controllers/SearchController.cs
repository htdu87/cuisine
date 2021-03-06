﻿using Cuisine.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace Cuisine.Controllers
{
    
    public class SearchController : Controller
    {
        private SparqlParameterizedString queryString;
        private LeviathanQueryProcessor process;
        private IDictionary dic;
        private IDictionary dicMaterial;
        private IGraph g;

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

            string dsPath = Server.MapPath("~/App_Data/CusineThao.owl");
            //string dsPath = Server.MapPath("~/App_Data/db.owl");
            g = new Graph();
            g.LoadFromFile(dsPath);

            InMemoryDataset ds = new InMemoryDataset(g);
            process = new LeviathanQueryProcessor(ds);

            queryString = new SparqlParameterizedString();
            queryString.Namespaces.AddNamespace("owl", new Uri("http://www.w3.org/2002/07/owl#"));
            queryString.Namespaces.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            queryString.Namespaces.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            queryString.Namespaces.AddNamespace("xsd", new Uri("http://www.w3.org/2001/XMLSchema#"));
            queryString.Namespaces.AddNamespace("cuisine", new Uri("http://www.semanticweb.org/latitude/ontologies/2021/1/untitled-ontology-12#"));
            //queryString.Namespaces.AddNamespace("cuisine", new Uri("http://www.semanticweb.org/du/ontologies/2021/1/cuisine#"));

            // Lay danh sach khu vuc
            queryString.CommandText = "select * where {?x rdf:type cuisine:KhuVuc . ?x cuisine:coTenKhuVuc ?tenKV} ORDER BY ?tenKV";

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(queryString);

            SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);

            dic = new Dictionary<string, string>();
            dicMaterial = new Dictionary<string, string>();

            foreach (SparqlResult result in resultSet)
            {
                IUriNode nodeX = (IUriNode)result.Value("x");
                ILiteralNode nodeTenKV = (ILiteralNode)result.Value("tenKV");

                dic[nodeX.Uri.Fragment.Remove(0, 1)] = nodeTenKV.Value;
            }

            // Lay danh sach nguyen lieu
            queryString.CommandText = "select * where {?x rdf:type cuisine:NguyenLieu ; cuisine:coTenNguyenLieu ?tnl} ORDER BY ?x";

            sparqlparser = new SparqlQueryParser();
            query = sparqlparser.ParseFromString(queryString);

            resultSet = (SparqlResultSet)process.ProcessQuery(query);

            foreach (SparqlResult result in resultSet)
            {
                IUriNode nodeX = (IUriNode)result.Value("x");
                ILiteralNode nodeTenNL = (ILiteralNode)result.Value("tnl");

                dicMaterial[nodeX.Uri.Fragment.Remove(0, 1)] = nodeTenNL.Value;
            }
        }

        // GET: Search
        public ActionResult Index()
        {

            ViewBag.dsKhuVuc = dic;

            return View();
        }

        [HttpPost]
        public ActionResult Search(string keyword, string chay)
        {
            List<Dish> listDish = new List<Dish>();

            ViewBag.keyword = keyword;
            ViewBag.dsKhuVuc = dic;

            string comText = "select * where {?x rdf:type cuisine:MonAn . ?x cuisine:thuocKhuVuc cuisine:" + keyword + " ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:coID ?id . } ORDER BY ?ten";

            if (!string.IsNullOrEmpty(chay) && chay == "true")
                comText = "select * where {?x rdf:type cuisine:MonAn . ?x cuisine:thuocKhuVuc cuisine:" + keyword + " ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:coID ?id . FILTER(?chay = true) . }  ORDER BY ?ten";

            queryString.CommandText = comText;

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(queryString);

            SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);

            if (resultSet.Count > 0)
            {
                foreach (SparqlResult result in resultSet)
                {
                    IUriNode nodeX = (IUriNode)result.Value("x");
                    ILiteralNode nodeName = (ILiteralNode)result.Value("ten");
                    ILiteralNode nodeDesc = (ILiteralNode)result.Value("mota");
                    ILiteralNode nodePicture = (ILiteralNode)result.Value("anh");
                    ILiteralNode nodeChay = (ILiteralNode)result.Value("chay");
                    ILiteralNode nodeID = (ILiteralNode)result.Value("id");

                    Dish dish = new Dish();
                    dish.Desc = nodeDesc.Value;
                    dish.Uri = nodeX.Uri.Fragment.Remove(0, 1);
                    dish.Name = nodeName.Value;
                    dish.Picture = nodePicture.Value;
                    dish.Chay = nodeChay.Value == "true";
                    dish.Id = long.Parse(nodeID.Value);

                    listDish.Add(dish);
                }

                ViewBag.searchResult = listDish;
            }
            else
            {
                ViewBag.Message = "Không tìm thấy món ăn nào, vui lòng thử lại với các khu vực khác!";
            }

            return View();
        }

        public ActionResult Detail(string id)
        {
            int _id;

            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out _id))
            {
                queryString.CommandText = "select * where {?x rdf:type cuisine:MonAn . ?x cuisine:coID ?id ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:thuocKhuVuc ?kv . FILTER(?id = " + id + ") }";

                SparqlQueryParser sparqlparser = new SparqlQueryParser();
                SparqlQuery query = sparqlparser.ParseFromString(queryString);

                SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);
                if(resultSet.Count>0)
                {
                    // Lay thong tin mon an
                    SparqlResult result = resultSet[0];

                    IUriNode nodeX = (IUriNode)result.Value("x");
                    ILiteralNode nodeName = (ILiteralNode)result.Value("ten");
                    ILiteralNode nodeDesc = (ILiteralNode)result.Value("mota");
                    ILiteralNode nodePicture = (ILiteralNode)result.Value("anh");
                    ILiteralNode nodeChay = (ILiteralNode)result.Value("chay");
                    ILiteralNode nodeID = (ILiteralNode)result.Value("id");
                    IUriNode nodeKV = (IUriNode)result.Value("kv");

                    Dish dish = new Dish();
                    dish.Desc = nodeDesc.Value;
                    dish.Uri = nodeX.Uri.Fragment.Remove(0, 1);
                    dish.Name = nodeName.Value;
                    dish.Picture = nodePicture.Value;
                    dish.Chay = nodeChay.Value == "true";
                    dish.Id = long.Parse(nodeID.Value);

                    ViewBag.dish = dish;

                    // Lay thanh phan mon an
                    List<ThanhPhan> congThuc = new List<ThanhPhan>();

                    queryString.CommandText = "select ?tennl ?sl ?dvt where {?x rdf:type cuisine:MonAn . ?x cuisine:coID ?id . ?x cuisine:coThanhPhan ?tp . ?tp cuisine:coDVT ?dvt . ?tp cuisine:coSoLuong ?sl . ?tp cuisine:coNguyenLieu ?nl . ?nl cuisine:coTenNguyenLieu ?tennl . FILTER(?id = " + id + ") }";
                    query = sparqlparser.ParseFromString(queryString);
                    resultSet = (SparqlResultSet)process.ProcessQuery(query);

                    foreach (SparqlResult _result in resultSet)
                    {
                        ILiteralNode nodeTen = (ILiteralNode)_result.Value("tennl");
                        ILiteralNode nodeSL = (ILiteralNode)_result.Value("sl");
                        ILiteralNode nodeDVT = (ILiteralNode)_result.Value("dvt");

                        ThanhPhan tp = new ThanhPhan();
                        tp.DVT = nodeDVT.Value;
                        tp.Ten = nodeTen.Value;
                        tp.SoLuong = float.Parse(nodeSL.Value);

                        congThuc.Add(tp);
                    }

                    ViewBag.congThuc = congThuc;

                    // Lay mon an cung khu vuc
                    List<Dish> relatedDishes = new List<Dish>();
                    queryString.CommandText = "select * where {  ?x rdf:type cuisine:MonAn . ?x cuisine:coID ?id ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:thuocKhuVuc ?kv . FILTER(?id != " + id + " && ?kv = cuisine:"+nodeKV.Uri.Fragment.Remove(0, 1) + ") } ORDER BY RAND() LIMIT 4";

                    sparqlparser = new SparqlQueryParser();
                    query = sparqlparser.ParseFromString(queryString);

                    resultSet = (SparqlResultSet)process.ProcessQuery(query);
                    foreach (SparqlResult _result in resultSet)
                    {
                        IUriNode _nodeX = (IUriNode)_result.Value("x");
                        ILiteralNode _nodeName = (ILiteralNode)_result.Value("ten");
                        ILiteralNode _nodeDesc = (ILiteralNode)_result.Value("mota");
                        ILiteralNode _nodePicture = (ILiteralNode)_result.Value("anh");
                        ILiteralNode _nodeChay = (ILiteralNode)_result.Value("chay");
                        ILiteralNode _nodeID = (ILiteralNode)_result.Value("id");

                        Dish _dish = new Dish();
                        _dish.Desc = _nodeDesc.Value;
                        _dish.Uri = _nodeX.Uri.Fragment.Remove(0, 1);
                        _dish.Name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(_nodeName.Value.ToLower());
                        _dish.Picture = _nodePicture.Value;
                        _dish.Chay = _nodeChay.Value == "true";
                        _dish.Id = long.Parse(_nodeID.Value);

                        relatedDishes.Add(_dish);
                    }

                    ViewBag.relatedDishes = relatedDishes;
                }
                else
                {
                    ViewBag.Message = "Không tìm thấy món ăn, vui lòng kiểm tra lại!!!";
                }
            }
            else
            {
                ViewBag.Message = "Yêu cầu xem chi tiết thông tin món ăn không hợp lệ!!!";
            }

            return View();
        }

        public ActionResult Material()
        {
            

            ViewBag.dicMaterial = dicMaterial;

            return View();
        }

        [HttpPost]
        public ActionResult SearchMaterial(string keyword, string chay)
        {
            List<Dish> listDish = new List<Dish>();

            ViewBag.keyword = keyword;
            ViewBag.dicMaterial = dicMaterial;

            string comText = "select * where {?x rdf:type cuisine:MonAn ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:coID ?id ; cuisine:coThanhPhan ?tp . ?tp cuisine:coNguyenLieu ?nl . FILTER(cuisine:"+keyword+" IN (?nl)) } ORDER BY ?ten";

            if (!string.IsNullOrEmpty(chay) && chay == "true")
                comText = "select * where {?x rdf:type cuisine:MonAn ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:coID ?id ; cuisine:coThanhPhan ?tp . ?tp cuisine:coNguyenLieu ?nl . FILTER(cuisine:" + keyword + " IN (?nl) && ?chay = true) } ORDER BY ?ten";

            queryString.CommandText = comText;

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(queryString);

            SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);

            if (resultSet.Count > 0)
            {
                foreach (SparqlResult result in resultSet)
                {
                    IUriNode nodeX = (IUriNode)result.Value("x");
                    ILiteralNode nodeName = (ILiteralNode)result.Value("ten");
                    ILiteralNode nodeDesc = (ILiteralNode)result.Value("mota");
                    ILiteralNode nodePicture = (ILiteralNode)result.Value("anh");
                    ILiteralNode nodeChay = (ILiteralNode)result.Value("chay");
                    ILiteralNode nodeID = (ILiteralNode)result.Value("id");

                    Dish dish = new Dish();
                    dish.Desc = nodeDesc.Value;
                    dish.Uri = nodeX.Uri.Fragment.Remove(0, 1);
                    dish.Name = nodeName.Value;
                    dish.Picture = nodePicture.Value;
                    dish.Chay = nodeChay.Value == "true";
                    dish.Id = long.Parse(nodeID.Value);

                    listDish.Add(dish);
                }

                ViewBag.searchResult = listDish;
            }
            else
            {
                ViewBag.Message = "Không tìm thấy món ăn nào, vui lòng thử lại với các nguyên liệu khác!";
            }
            return View();
        }
    }
}