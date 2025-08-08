using AutoMapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Mappings
{
    public class EntidadeProfile : Profile
    {
        public EntidadeProfile()
        {
            CreateMap<EntidadeViewModel, Entidade>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<Entidade, EntidadeViewModel>();
        }
    }
}